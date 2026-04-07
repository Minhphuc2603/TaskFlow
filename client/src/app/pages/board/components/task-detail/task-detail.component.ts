import { Component, Input, Output, EventEmitter, OnInit, OnChanges, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TaskItem, TaskComment, Priority, UpdateTaskRequest } from '../../../../models/project.model';
import { BoardService } from '../../../../services/board.service';

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
  @Output() close = new EventEmitter<void>();
  @Output() updated = new EventEmitter<TaskItem>();

  comments = signal<TaskComment[]>([]);
  isLoadingComments = signal(false);
  newComment = '';
  editingTitle = false;
  editTitle = '';
  editDescription = '';
  editPriority: Priority = Priority.None;
  editDueDate = '';

  Priority = Priority;

  priorityOptions = [
    { value: Priority.None, label: 'Không', color: '#64748b', icon: '—' },
    { value: Priority.Low, label: 'Thấp', color: '#22c55e', icon: '↓' },
    { value: Priority.Medium, label: 'Trung bình', color: '#f59e0b', icon: '→' },
    { value: Priority.High, label: 'Cao', color: '#f97316', icon: '↑' },
    { value: Priority.Critical, label: 'Khẩn cấp', color: '#ef4444', icon: '⚡' },
  ];

  constructor(private boardService: BoardService) { }

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
      this.saveField({ dueDate: this.editDueDate });
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
}
