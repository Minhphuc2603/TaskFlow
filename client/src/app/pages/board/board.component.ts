import { Component, OnInit, signal, ViewChildren, QueryList, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CdkDragDrop, DragDropModule, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { BoardService } from '../../services/board.service';
import { Board, TaskItem, Priority, BoardColumn } from '../../models/project.model';
import { TaskCardComponent } from './components/task-card/task-card.component';
import { ConfirmDialogComponent } from './components/confirm-dialog/confirm-dialog.component';
import { ThemeService } from '../../services/theme.service';

@Component({
  selector: 'app-board',
  standalone: true,
  imports: [CommonModule, DragDropModule, RouterLink, FormsModule, TaskCardComponent, ConfirmDialogComponent],
  templateUrl: './board.component.html',
  styleUrl: './board.component.scss'
})
export class BoardComponent implements OnInit {
  board = signal<Board | null>(null);
  isLoading = signal(true);
  addingToColumn = signal<string | null>(null);
  taskToDelete = signal<{taskId: string, columnId: string} | null>(null);
  newTaskTitle = '';

  @ViewChildren('taskInput') taskInputs!: QueryList<ElementRef>;

  constructor(
    private route: ActivatedRoute,
    private boardService: BoardService,
    public themeService: ThemeService
  ) {}

  ngOnInit(): void {
    const projectId = this.route.snapshot.paramMap.get('id');
    if (projectId) {
      this.boardService.getBoardsByProject(projectId).subscribe({
        next: (boards) => {
          if (boards && boards.length > 0) {
            // Sort columns logically based on order
            boards[0].columns.sort((a, b) => a.order - b.order);
            // Sort tasks inside each column
            boards[0].columns.forEach(c => c.tasks.sort((t1, t2) => t1.order - t2.order));
            
            this.board.set(boards[0]);
          }
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
        }
      });
    }
  }

  drop(event: CdkDragDrop<TaskItem[]>, targetColumnId: string) {
    if (event.previousContainer === event.container) {
      // Reordering within the same column
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      // Moving to a different column
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex,
      );
    }

    // Assign the new column ID to the task visually
    const movedTask = event.container.data[event.currentIndex];
    movedTask.columnId = targetColumnId;

    // Call API to sync state
    this.boardService.moveTask(movedTask.id, targetColumnId, event.currentIndex).subscribe({
      next: () => {
        // Success silently
      },
      error: (err) => {
        console.error('Failed to move task', err);
        // Ideally we should revert the UI state here if it fails
      }
    });
  }

  startAddTask(columnId: string) {
    this.addingToColumn.set(columnId);
    this.newTaskTitle = '';
    // Focus the input after view updates
    setTimeout(() => {
      const inputs = this.taskInputs?.toArray();
      if (inputs && inputs.length > 0) {
        inputs[inputs.length - 1].nativeElement.focus();
      }
    }, 50);
  }

  cancelAddTask() {
    this.addingToColumn.set(null);
    this.newTaskTitle = '';
  }

  submitCreateTask(columnId: string) {
    const title = this.newTaskTitle.trim();
    if (!title) return;

    this.boardService.createTask(columnId, title).subscribe({
      next: (newTask) => {
        const board = this.board();
        if (board) {
          const column = board.columns.find(c => c.id === columnId);
          if (column) {
            column.tasks.push(newTask);
          }
        }
        // Keep form open for quick multi-add
        this.newTaskTitle = '';
        setTimeout(() => {
          const inputs = this.taskInputs?.toArray();
          if (inputs && inputs.length > 0) {
            inputs[inputs.length - 1].nativeElement.focus();
          }
        }, 50);
      },
      error: (err) => console.error('Create task failed', err)
    });
  }

  deleteTask(taskId: string, columnId: string) {
    this.taskToDelete.set({ taskId, columnId });
  }

  cancelDelete() {
    this.taskToDelete.set(null);
  }

  confirmDelete() {
    const toDelete = this.taskToDelete();
    if (!toDelete) return;

    this.boardService.deleteTask(toDelete.taskId).subscribe({
      next: () => {
        const board = this.board();
        if (board) {
          const column = board.columns.find(c => c.id === toDelete.columnId);
          if (column) {
            column.tasks = column.tasks.filter(t => t.id !== toDelete.taskId);
          }
        }
        this.taskToDelete.set(null);
      },
      error: (err) => {
        console.error('Delete task failed', err);
        this.taskToDelete.set(null);
      }
    });
  }
}

