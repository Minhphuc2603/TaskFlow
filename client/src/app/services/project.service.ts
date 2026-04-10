import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Project, CreateProject, ProjectMember, UserSearchResult } from '../models/project.model';

@Injectable({
  providedIn: 'root'
})
export class ProjectService {
  private apiUrl = `${environment.apiUrl}/projects`;

  constructor(private http: HttpClient) {}

  getMyProjects(): Observable<Project[]> {
    return this.http.get<Project[]>(this.apiUrl);
  }

  getProject(id: string): Observable<Project> {
    return this.http.get<Project>(`${this.apiUrl}/${id}`);
  }

  createProject(data: CreateProject): Observable<Project> {
    return this.http.post<Project>(this.apiUrl, data);
  }

  updateProject(id: string, data: CreateProject): Observable<Project> {
    return this.http.put<Project>(`${this.apiUrl}/${id}`, data);
  }

  deleteProject(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getMembers(projectId: string): Observable<ProjectMember[]> {
    return this.http.get<ProjectMember[]>(`${this.apiUrl}/${projectId}/members`);
  }

  addMember(projectId: string, email: string): Observable<ProjectMember> {
    return this.http.post<ProjectMember>(`${this.apiUrl}/${projectId}/members`, { email });
  }

  removeMember(projectId: string, memberId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${projectId}/members/${memberId}`);
  }

  searchUsers(query: string, projectId: string): Observable<UserSearchResult[]> {
    return this.http.get<UserSearchResult[]>(`${this.apiUrl}/users/search`, {
      params: { q: query, projectId }
    });
  }
}
