import { Component, Input, Output, EventEmitter, OnInit, OnChanges, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TaskItem, TaskComment, Priority, UpdateTaskRequest, ProjectMember } from '../../../../models/project.model';
import { BoardService } from '../../../../services/board.service';
import { AuthService } from '../../../../services/auth.service';

@Component({
  selector: 'app-task-detail',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './task-detail.component.html',
  styleUrl: './task-detail.component.scss'
})
export class TaskDetailComponent implements OnInit, OnChanges {
  @Input() task!: TaskItem;
  @Input() columnName: string = '';
  @Input() members: ProjectMember[] = [];
  @Output() close = new EventEmitter<void>();
  @Output() updated = new EventEmitter<TaskItem>();

  comments = signal<TaskComment[]>([]);
  isLoadingComments = signal(false);
  newComment = '';
  editingCommentId: string | null = null;
  editCommentContent = '';
  editingTitle = false;
  editTitle = '';
  editDescription = '';
  editPriority: Priority = Priority.None;
  editDueDate = '';
  showAssigneeDropdown = false;
  showLabelPopup = false;
  newLabelName = "";
  newChecklistTitle = '';
  presetColors = [
    '#ef4444', '#f97316', '#f59e0b', '#84cc16',
    '#10b981', '#06b6d4', '#3b82f6', '#8b5cf6',
    '#ec4899', '#64748b'
  ];
  selectedColor = this.presetColors[7];
  Priority = Priority;

  priorityOptions = [
    { value: Priority.None, label: 'Không', color: '#64748b', icon: '—' },
    { value: Priority.Low, label: 'Thấp', color: '#22c55e', icon: '↓' },
    { value: Priority.Medium, label: 'Trung bình', color: '#f59e0b', icon: '→' },
    { value: Priority.High, label: 'Cao', color: '#f97316', icon: '↑' },
    { value: Priority.Critical, label: 'Khẩn cấp', color: '#ef4444', icon: '⚡' },
  ];

  constructor(private boardService: BoardService, private authService: AuthService) { }

  get currentUserId(): string | null {
    return this.authService.currentUser()?.userId ?? null;
  }

  ngOnInit(): void {
    this.syncFromTask();
    this.loadComments();
  }

  ngOnChanges(): void {
    this.syncFromTask();
    this.loadComments();
  }

  private syncFromTask() {
    this.editTitle = this.task.title;
    this.editDescription = this.task.description || '';
    this.editPriority = this.task.priority;
    this.editDueDate = this.task.dueDate ? this.task.dueDate.substring(0, 10) : '';
  }

  loadComments() {
    this.isLoadingComments.set(true);
    this.boardService.getComments(this.task.id).subscribe({
      next: (comments) => {
        this.comments.set(comments);
        this.isLoadingComments.set(false);
      },
      error: () => this.isLoadingComments.set(false)
    });
  }

  startEditTitle() {
    this.editingTitle = true;
  }

  saveTitle() {
    this.editingTitle = false;
    if (this.editTitle.trim() && this.editTitle !== this.task.title) {
      this.saveField({ title: this.editTitle.trim() });
    }
  }

  saveDescription() {
    if (this.editDescription !== (this.task.description || '')) {
      this.saveField({ description: this.editDescription });
    }
  }

  savePriority(priority: Priority) {
    this.editPriority = priority;
    this.saveField({ priority });
  }

  saveDueDate() {
    if (this.editDueDate) {
      // Convert to ISO string to ensure the .NET backend correctly parses the date
      const isoDate = new Date(this.editDueDate).toISOString();
      this.saveField({ dueDate: isoDate });
    } else {
      this.saveField({ clearDueDate: true });
    }
  }

  private saveField(data: UpdateTaskRequest) {
    this.boardService.updateTask(this.task.id, data).subscribe({
      next: (updatedTask) => {
        Object.assign(this.task, updatedTask);
        this.updated.emit(updatedTask);
      },
      error: (err) => console.error('Update failed', err)
    });
  }

  submitComment() {
    const content = this.newComment.trim();
    if (!content) return;

    this.boardService.addComment(this.task.id, content).subscribe({
      next: (comment) => {
        this.comments.update(c => [comment, ...c]);
        this.newComment = '';
        this.task.commentCount++;
      },
      error: (err) => console.error('Add comment failed', err)
    });
  }

  startEditComment(comment: TaskComment) {
    this.editingCommentId = comment.id;
    this.editCommentContent = comment.content;
  }

  saveCommentEdit(comment: TaskComment) {
    const content = this.editCommentContent.trim();
    if (!content || content === comment.content) {
      this.cancelCommentEdit();
      return;
    }
    this.boardService.updateComment(this.task.id, comment.id, content).subscribe({
      next: (updated) => {
        this.comments.update(list =>
          list.map(c => c.id === updated.id ? updated : c)
        );
        this.cancelCommentEdit();
      },
      error: (err) => console.error('Update comment failed', err)
    });
  }

  cancelCommentEdit() {
    this.editingCommentId = null;
    this.editCommentContent = '';
  }

  deleteComment(commentId: string) {
    this.boardService.deleteComment(this.task.id, commentId).subscribe({
      next: () => {
        this.comments.update(list => list.filter(c => c.id !== commentId));
        this.task.commentCount = Math.max(0, this.task.commentCount - 1);
      },
      error: (err) => console.error('Delete comment failed', err)
    });
  }

  getPriorityOption(priority: Priority) {
    return this.priorityOptions.find(p => p.value === priority) || this.priorityOptions[0];
  }

  getTimeAgo(dateStr: string): string {
    const diff = Date.now() - new Date(dateStr).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return 'Vừa xong';
    if (mins < 60) return `${mins} phút trước`;
    const hours = Math.floor(mins / 60);
    if (hours < 24) return `${hours} giờ trước`;
    const days = Math.floor(hours / 24);
    return `${days} ngày trước`;
  }

  onOverlayClick(event: Event) {
    if ((event.target as HTMLElement).classList.contains('detail-overlay')) {
      this.close.emit();
    }
  }

  saveAssignee(userId: string | null) {
    this.showAssigneeDropdown = false;
    if (userId) {
      const member = this.members.find(m => m.userId === userId);
      this.saveField({ assigneeId: userId });
      // Optimistic update
      this.task.assigneeId = userId;
      this.task.assigneeName = member?.fullName || undefined;
    } else {
      this.saveField({ clearAssignee: true });
      this.task.assigneeId = undefined;
      this.task.assigneeName = undefined;
    }
  }

  getAssigneeName(): string {
    if (!this.task.assigneeId) return '';
    const member = this.members.find(m => m.userId === this.task.assigneeId);
    return member?.fullName || this.task.assigneeName || 'Unknown';
  }

  openLabelPopup() {
    this.showLabelPopup = !this.showLabelPopup;
    this.newLabelName = "";
  }

  addLabel() {
    if (!this.newLabelName.trim()) return;
    this.boardService.addTaskLabel(this.task.id, this.newLabelName, this.selectedColor).subscribe({
      next: (newLablel) => {
        this.task.labels.push(newLablel);
        this.showLabelPopup = false
      }
    })
  }

  deleteLabel(labelId: string) {
    this.boardService.deleteTaskLabel(this.task.id, labelId)
      .subscribe({
        next: () => {
          this.task.labels = this.task.labels.filter(l => l.id !== labelId);
        }
      });
  }

  get checklistProgress(): number {
    if (!this.task.checklists || this.task.checklists.length === 0) return 0;
    const completed = this.task.checklists.filter(c => c.isCompleted).length;
    return Math.round((completed / this.task.checklists.length) * 100);
  }

  addChecklist() {
    if (!this.newChecklistTitle.trim()) return;
    this.boardService.addChecklist(this.task.id, this.newChecklistTitle.trim()).subscribe({
      next: (checklist) => {
        if (!this.task.checklists) this.task.checklists = [];
        this.task.checklists.push(checklist);
        this.newChecklistTitle = '';
      }
    });
  }

  toggleChecklist(checklist: any) {
    const newVal = !checklist.isCompleted;
    this.boardService.updateChecklist(this.task.id, checklist.id, checklist.title, newVal).subscribe({
      next: (updated) => {
        checklist.isCompleted = updated.isCompleted;
      }
    });
  }

  deleteChecklist(checklistId: string) {
    this.boardService.deleteChecklist(this.task.id, checklistId).subscribe({
      next: () => {
        this.task.checklists = this.task.checklists.filter(c => c.id !== checklistId);
      }
    });
  }
}
