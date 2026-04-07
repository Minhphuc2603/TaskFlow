import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Board, TaskItem, TaskComment, UpdateTaskRequest } from '../models/project.model';

@Injectable({
  providedIn: 'root'
})
export class BoardService {
  private apiUrl = `${environment.apiUrl}/boards`;

  constructor(private http: HttpClient) {}

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
}
