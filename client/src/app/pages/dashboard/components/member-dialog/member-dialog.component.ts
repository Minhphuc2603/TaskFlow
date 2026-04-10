import { Component, Input, Output, EventEmitter, OnInit, signal, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, switchMap, of } from 'rxjs';
import { ProjectService } from '../../../../services/project.service';
import { ProjectMember, UserSearchResult } from '../../../../models/project.model';
import { AuthService } from '../../../../services/auth.service';

@Component({
  selector: 'app-member-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './member-dialog.component.html',
  styleUrl: './member-dialog.component.scss'
})
export class MemberDialogComponent implements OnInit, OnDestroy {
  @Input({ required: true }) projectId!: string;
  @Input() isOwner: boolean = false;
  @Output() close = new EventEmitter<void>();

  members = signal<ProjectMember[]>([]);
  searchResults = signal<UserSearchResult[]>([]);
  isLoading = signal(true);
  isSearching = signal(false);
  searchQuery = '';
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  private searchSubject = new Subject<string>();

  constructor(
    private projectService: ProjectService,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadMembers();

    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(query => {
        if (query.trim().length < 2) {
          return of([]);
        }
        this.isSearching.set(true);
        return this.projectService.searchUsers(query, this.projectId);
      })
    ).subscribe({
      next: (results) => {
        this.searchResults.set(results);
        this.isSearching.set(false);
      },
      error: () => this.isSearching.set(false)
    });
  }

  ngOnDestroy(): void {
    this.searchSubject.complete();
  }

  loadMembers(): void {
    this.isLoading.set(true);
    this.projectService.getMembers(this.projectId).subscribe({
      next: (members) => {
        this.members.set(members);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  onSearchInput(): void {
    this.searchSubject.next(this.searchQuery);
  }

  addMember(email: string): void {
    this.errorMessage.set(null);
    this.projectService.addMember(this.projectId, email).subscribe({
      next: (member) => {
        this.members.update(m => [...m, member]);
        this.searchResults.update(r => r.filter(u => u.email !== email));
        this.successMessage.set(`Đã thêm ${member.fullName}`);
        setTimeout(() => this.successMessage.set(null), 3000);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Có lỗi xảy ra');
        setTimeout(() => this.errorMessage.set(null), 3000);
      }
    });
  }

  removeMember(member: ProjectMember): void {
    this.errorMessage.set(null);
    this.projectService.removeMember(this.projectId, member.id).subscribe({
      next: () => {
        this.members.update(m => m.filter(x => x.id !== member.id));
        this.successMessage.set(`Đã xóa ${member.fullName}`);
        setTimeout(() => this.successMessage.set(null), 3000);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || 'Có lỗi xảy ra');
        setTimeout(() => this.errorMessage.set(null), 3000);
      }
    });
  }

  isCurrentUser(userId: string): boolean {
    return this.authService.currentUser()?.userId === userId;
  }

  getRoleBadgeClass(role: string): string {
    switch (role) {
      case 'Owner': return 'role-owner';
      case 'Admin': return 'role-admin';
      default: return 'role-member';
    }
  }

  onOverlayClick(event: Event): void {
    if ((event.target as HTMLElement).classList.contains('member-overlay')) {
      this.close.emit();
    }
  }
}
