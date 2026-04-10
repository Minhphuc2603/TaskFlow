import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ProjectService } from '../../services/project.service';
import { Project } from '../../models/project.model';
import { ConfirmDialogComponent } from '../board/components/confirm-dialog/confirm-dialog.component';
import { MemberDialogComponent } from './components/member-dialog/member-dialog.component';
import { ThemeService } from '../../services/theme.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, ConfirmDialogComponent, MemberDialogComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  projects = signal<Project[]>([]);
  isLoading = signal(true);
  showCreateModal = signal(false);
  newProjectName = '';
  newProjectDescription = '';
  projectToDelete = signal<Project | null>(null);
  memberProject = signal<Project | null>(null);

  private gradients = [
    'linear-gradient(135deg, #6366f1, #8b5cf6)',
    'linear-gradient(135deg, #ec4899, #f43f5e)',
    'linear-gradient(135deg, #14b8a6, #06b6d4)',
    'linear-gradient(135deg, #f59e0b, #ef4444)',
    'linear-gradient(135deg, #10b981, #059669)',
    'linear-gradient(135deg, #3b82f6, #6366f1)',
  ];

  constructor(
    public authService: AuthService,
    private projectService: ProjectService,
    private router: Router,
    public themeService: ThemeService
  ) {}

  ngOnInit(): void {
    this.loadProjects();
  }

  loadProjects(): void {
    this.projectService.getMyProjects().subscribe({
      next: (projects) => {
        this.projects.set(projects);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  createProject(): void {
    if (!this.newProjectName.trim()) return;

    this.projectService.createProject({
      name: this.newProjectName,
      description: this.newProjectDescription
    }).subscribe({
      next: (project) => {
        this.projects.update(p => [project, ...p]);
        this.showCreateModal.set(false);
        this.newProjectName = '';
        this.newProjectDescription = '';
      }
    });
  }

  openProject(project: Project): void {
    this.router.navigate(['/projects', project.id]);
  }

  startDeleteProject(event: Event, project: Project): void {
    event.stopPropagation();
    this.projectToDelete.set(project);
  }

  cancelDeleteProject(): void {
    this.projectToDelete.set(null);
  }

  confirmDeleteProject(): void {
    const project = this.projectToDelete();
    if (!project) return;

    this.projectService.deleteProject(project.id).subscribe({
      next: () => {
        this.projects.update(projects => projects.filter(p => p.id !== project.id));
        this.projectToDelete.set(null);
      },
      error: (err) => {
        console.error('Delete project failed', err);
        this.projectToDelete.set(null);
      }
    });
  }

  getProjectGradient(project: Project): string {
    const index = project.name.charCodeAt(0) % this.gradients.length;
    return this.gradients[index];
  }

  getUserInitials(): string {
    const name = this.authService.currentUser()?.fullName || '';
    return name.split(' ').map(n => n[0]).join('').toUpperCase().substring(0, 2);
  }

  openMembers(event: Event, project: Project): void {
    event.stopPropagation();
    this.memberProject.set(project);
  }

  logout(): void {
    this.authService.logout();
  }
}
