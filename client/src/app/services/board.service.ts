import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Board, TaskItem, TaskComment, UpdateTaskRequest, BoardColumn, TaskLabel, TaskChecklist } from '../models/project.model';

@Injectable({
  providedIn: 'root'
})
export class BoardService {
  private apiUrl = `${environment.apiUrl}/boards`;

  constructor(private http: HttpClient) { }

  getBoard(id: string): Observable<Board> {
    return this.http.get<Board>(`${this.apiUrl}/${id}`);
  }

  getBoardsByProject(projectId: string): Observable<Board[]> {
    return this.http.get<Board[]>(`${this.apiUrl}/project/${projectId}`);
  }

  createBoard(projectId: string, name: string): Observable<Board> {
    return this.http.post<Board>(`${this.apiUrl}/project/${projectId}`, { name });
  }

  moveTask(taskId: string, targetColumnId: string, newOrder: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/tasks/${taskId}/move`, {
      targetColumnId,
      newOrder
    });
  }

  createTask(columnId: string, title: string, description?: string): Observable<TaskItem> {
    return this.http.post<TaskItem>(`${this.apiUrl}/columns/${columnId}/tasks`, {
      title,
      description
    });
  }

  deleteTask(taskId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/tasks/${taskId}`);
  }

  updateTask(taskId: string, data: UpdateTaskRequest): Observable<TaskItem> {
    return this.http.put<TaskItem>(`${this.apiUrl}/tasks/${taskId}`, data);
  }

  getComments(taskId: string): Observable<TaskComment[]> {
    return this.http.get<TaskComment[]>(`${this.apiUrl}/tasks/${taskId}/comments`);
  }

  addComment(taskId: string, content: string): Observable<TaskComment> {
    return this.http.post<TaskComment>(`${this.apiUrl}/tasks/${taskId}/comments`, { content });
  }

  updateComment(taskId: string, commentId: string, content: string): Observable<TaskComment> {
    return this.http.put<TaskComment>(`${this.apiUrl}/tasks/${taskId}/comments/${commentId}`, { content });
  }

  deleteComment(taskId: string, commentId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/tasks/${taskId}/comments/${commentId}`);
  }

  deleteColumn(columnId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/columns/${columnId}`);
  }

  addColumn(boardId: string, name: string, color: string): Observable<BoardColumn> {
    return this.http.post<BoardColumn>(`${this.apiUrl}/${boardId}/columns`, {
      name,
      color
    });
  }
  addTaskLabel(taskId: string, name: string, color: string): Observable<TaskLabel> {
    return this.http.post<TaskLabel>(`${this.apiUrl}/tasks/${taskId}/labels`, { name, color });
  }

  deleteTaskLabel(taskId: string, labelId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/tasks/${taskId}/labels/${labelId}`);
  }

  addChecklist(taskId: string, title: string): Observable<TaskChecklist> {
    return this.http.post<TaskChecklist>(`${this.apiUrl}/tasks/${taskId}/checklists`, { title });
  }

  updateChecklist(taskId: string, checklistId: string, title: string, isCompleted: boolean): Observable<TaskChecklist> {
    return this.http.put<TaskChecklist>(`${this.apiUrl}/tasks/${taskId}/checklists/${checklistId}`, { title, isCompleted });
  }

  deleteChecklist(taskId: string, checklistId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/tasks/${taskId}/checklists/${checklistId}`);
  }
  moveColumn(boardId: string, columnId: string, newOrder: number): Observable<void> {
  return this.http.put<void>(`${this.apiUrl}/${boardId}/columns/${columnId}/move`, { newOrder });
}

}

