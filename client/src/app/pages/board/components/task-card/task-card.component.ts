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
      case Priority.Critical: return '#ef4444'; // Red
      case Priority.High: return '#fb923c';     // Orange
      case Priority.Medium: return '#fbbf24';   // Amber
      case Priority.Low: return '#3b82f6';      // Blue
      default: return '#475569'; // Slate 600 - Default grey
    }
  }
}
