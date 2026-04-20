import { Component, OnInit, signal, ViewChildren, QueryList, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CdkDragDrop, DragDropModule, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { BoardService } from '../../services/board.service';
import { ProjectService } from '../../services/project.service';
import { AuthService } from '../../services/auth.service';
import { Board, TaskItem, ProjectMember, BoardColumn } from '../../models/project.model';
import { TaskCardComponent } from './components/task-card/task-card.component';
import { ConfirmDialogComponent } from './components/confirm-dialog/confirm-dialog.component';
import { TaskDetailComponent } from './components/task-detail/task-detail.component';
import { MemberDialogComponent } from '../dashboard/components/member-dialog/member-dialog.component';
import { ThemeService } from '../../services/theme.service';

@Component({
  selector: 'app-board',
  standalone: true,
  imports: [CommonModule, DragDropModule, RouterLink, FormsModule, TaskCardComponent, ConfirmDialogComponent, TaskDetailComponent, MemberDialogComponent],
  templateUrl: './board.component.html',
  styleUrl: './board.component.scss'
})
export class BoardComponent implements OnInit {
  board = signal<Board | null>(null);
  isLoading = signal(true);
  addingToColumn = signal<string | null>(null);
  taskToDelete = signal<{ taskId: string, columnId: string } | null>(null);
  columnToDelete = signal<{ id: string, name: string } | null>(null);
  selectedTask = signal<{ task: TaskItem, columnName: string } | null>(null);
  members = signal<ProjectMember[]>([]);
  showMemberDialog = signal(false);
  isOwner = signal(false);
  newTaskTitle = '';

  isAddingColumn = signal(false);
  newColumnName = '';
  newColumnColor = '#475569';

  @ViewChildren('taskInput') taskInputs!: QueryList<ElementRef>;

  constructor(
    private route: ActivatedRoute,
    private boardService: BoardService,
    private projectService: ProjectService,
    private authService: AuthService,
    public themeService: ThemeService
  ) { }

  ngOnInit(): void {
    const projectId = this.route.snapshot.paramMap.get('id');
    if (projectId) {
      this.boardService.getBoardsByProject(projectId).subscribe({
        next: (boards) => {
          if (boards && boards.length > 0) {
            boards[0].columns.sort((a, b) => a.order - b.order);
            boards[0].columns.forEach(c => c.tasks.sort((t1, t2) => t1.order - t2.order));
            this.board.set(boards[0]);
          }
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
        }
      });

      // Load project members
      this.projectService.getMembers(projectId).subscribe({
        next: (members) => {
          this.members.set(members);
          // Check if current user is Owner
          const currentUserId = this.authService.currentUser()?.userId;
          const me = members.find(m => m.userId === currentUserId);
          this.isOwner.set(me?.role === 'Owner');
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

  startAddColumn() {
    this.isAddingColumn.set(true);
    this.newColumnName = '';
    this.newColumnColor = '#475569';
  }

  cancelAddColumn() {
    this.isAddingColumn.set(false);
    this.newColumnName = '';
  }

  submitCreateColumn() {
    const name = this.newColumnName.trim();
    if (!name) return;

    const board = this.board();
    if (!board) return;

    this.boardService.addColumn(board.id, name, this.newColumnColor).subscribe({
      next: (newCol) => {
        board.columns.push(newCol);
        this.isAddingColumn.set(false);
        this.newColumnName = '';
      },
      error: (err) => console.error('Create column failed', err)
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

  deleteColumn(columnId: string, columnName: string) {
    this.columnToDelete.set({ id: columnId, name: columnName });
  }

  cancelDeleteColumn() {
    this.columnToDelete.set(null);
  }

  confirmDeleteColumn() {
    const toDelete = this.columnToDelete();
    if (!toDelete) return;

    this.boardService.deleteColumn(toDelete.id).subscribe({
      next: () => {
        const board = this.board();
        if (board) {
          board.columns = board.columns.filter(c => c.id !== toDelete.id);
        }
        this.columnToDelete.set(null);
      },
      error: (err) => {
        console.error('Delete column failed', err);
        this.columnToDelete.set(null);
      }
    });
  }

  openTask(task: TaskItem, columnName: string) {
    this.selectedTask.set({ task, columnName });
  }

  closeDetail() {
    this.selectedTask.set(null);
  }

  onTaskUpdated(updatedTask: TaskItem) {
    const board = this.board();
    if (!board) return;
    for (const col of board.columns) {
      const idx = col.tasks.findIndex(t => t.id === updatedTask.id);
      if (idx !== -1) {
        col.tasks[idx] = { ...col.tasks[idx], ...updatedTask };
        break;
      }
    }
  }
  dropColumn(event: CdkDragDrop<BoardColumn[]>) {
  const currentBoard = this.board();
  if (!currentBoard || event.previousIndex === event.currentIndex) return;
  // 1. Cập nhật vị trí trên giao diện người dùng
  moveItemInArray(currentBoard.columns, event.previousIndex, event.currentIndex);
  // 2. Cập nhật thuộc tính '.order' đồng nhất
  currentBoard.columns.forEach((col, index) => col.order = index);
  // 3. Gọi server để lưu
  const movedColumn = currentBoard.columns[event.currentIndex];
  this.boardService.moveColumn(currentBoard.id, movedColumn.id, event.currentIndex)
    .subscribe({
      next: () => console.log('Đã cập nhật vị trí cột lên Server'),
      error: (err) => console.error('Lỗi khi đổi chỗ cột:', err)
    });
}

}
