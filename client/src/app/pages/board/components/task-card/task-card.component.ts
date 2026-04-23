import { Component, Input, Output, EventEmitter, HostBinding } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TaskItem, Priority } from '../../../../models/project.model';

@Component({
  selector: 'app-task-card',
  standalone: true,
  imports: [CommonModule],
  styleUrl: './task-card.component.scss',
  templateUrl: './task-card.component.html'
})
export class TaskCardComponent {
  @Input({ required: true }) task!: TaskItem;
  @Output() delete = new EventEmitter<void>();

  onDelete(event: Event) {
    event.stopPropagation();
    this.delete.emit();
  }

  @HostBinding('style.--accent-color')
  get accentColor(): string {
    return this.getPriorityColor(this.task?.priority);
  }

  getPriorityColor(priority: Priority): string {
    switch (priority) {
      case Priority.Critical: return '#ef4444';
      case Priority.High: return '#fb923c';
      case Priority.Medium: return '#fbbf24';
      case Priority.Low: return '#3b82f6';
      default: return '#475569';
    }
  }

  getPriorityIcon(priority: Priority): string {
    switch (priority) {
      case Priority.Critical: return '⚡';
      case Priority.High: return '↑';
      case Priority.Medium: return '→';
      case Priority.Low: return '↓';
      default: return '—';
    }
  }

  getPriorityLabel(priority: Priority): string {
    switch (priority) {
      case Priority.Critical: return 'Khẩn cấp';
      case Priority.High: return 'Cao';
      case Priority.Medium: return 'Trung bình';
      case Priority.Low: return 'Thấp';
      default: return '';
    }
  }

  isOverdue(dueDate: string): boolean {
    return new Date(dueDate) < new Date();
  }

  formatDueDate(dueDate: string): string {
    const d = new Date(dueDate);
    const now = new Date();
    const diff = Math.floor((d.getTime() - now.getTime()) / 86400000);
    if (diff === 0) return 'Hôm nay';
    if (diff === 1) return 'Ngày mai';
    if (diff === -1) return 'Hôm qua';
    return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' });
  }

  get checklistDone(): number {
    return this.task.checklists?.filter(c => c.isCompleted).length ?? 0;
  }
}
