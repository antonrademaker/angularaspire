export enum SessionStatus {
  Draft = 'Draft',
  Scheduled = 'Scheduled',
  Cancelled = 'Cancelled',
}

export enum SessionLevel {
  Beginner = 'Beginner',
  Intermediate = 'Intermediate',
  Advanced = 'Advanced',
}

export enum SubmissionStatus {
  NotSubmitted = 'NotSubmitted',
  Submitted = 'Submitted',
  UnderReview = 'UnderReview',
  Accepted = 'Accepted',
  Rejected = 'Rejected',
}

export interface SessionAssignment {
  id: string;
  sessionId: string;
  trackId: string;
  timeSlotId: string;
  roomConfigurationId?: string;
  roomSnapshot?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface Session {
  id: string;
  eventId: string;
  shortCode: string;
  title: string;
  abstract?: string;
  status: SessionStatus;
  isConfirmed: boolean;
  isPublished: boolean;
  duration: number;
  level: SessionLevel;
  language?: string;
  capacity?: number;
  submissionStatus: SubmissionStatus;
  createdAt: string;
  updatedAt?: string;
  assignments: SessionAssignment[];
}

export interface CreateSessionRequest {
  title: string;
  shortCode: string;
  abstract?: string;
  duration: number;
  level: SessionLevel;
  language?: string;
  capacity?: number;
}

export interface UpdateSessionRequest {
  title: string;
  shortCode: string;
  abstract?: string;
  duration: number;
  level: SessionLevel;
  language?: string;
  capacity?: number;
}

export interface AssignSessionRequest {
  trackId: string;
  timeSlotId: string;
  roomConfigurationId?: string;
}
