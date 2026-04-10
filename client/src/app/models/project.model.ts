export interface Project {
  id: string;
  name: string;
  description?: string;
  coverImageUrl?: string;
  createdAt: string;
  memberCount: number;
  boardCount: number;
}

export interface CreateProject {
  name: string;
  description?: string;
}

export interface Board {
  id: string;
  name: string;
  projectId: string;
  projectName?: string;
  columns: BoardColumn[];
}

export interface BoardColumn {
  id: string;
  name: string;
  color: string;
  order: number;
  tasks: TaskItem[];
}

export interface TaskItem {
  id: string;
  title: string;
  description?: string;
  order: number;
  priority: Priority;
  dueDate?: string;
  assigneeId?: string;
  assigneeName?: string;
  columnId: string;
  commentCount: number;
  labels: TaskLabel[];
}

export interface TaskLabel {
  id: string;
  name: string;
  color: string;
}

export enum Priority {
  None = 0,
  Low = 1,
  Medium = 2,
  High = 3,
  Critical = 4
}

export interface TaskComment {
  id: string;
  content: string;
  userId: string;
  userName: string;
  createdAt: string;
}

export interface UpdateTaskRequest {
  title?: string;
  description?: string;
  priority?: Priority;
  dueDate?: string | null;
  clearDueDate?: boolean;
  assigneeId?: string | null;
  clearAssignee?: boolean;
}

export interface ProjectMember {
  id: string;
  userId: string;
  fullName: string;
  email: string;
  role: string;
  joinedAt: string;
}

export interface UserSearchResult {
  userId: string;
  fullName: string;
  email: string;
}
