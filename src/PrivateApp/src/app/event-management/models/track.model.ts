export interface Track {
  id: string;
  eventId: string;
  name: string;
  description?: string;
  color?: string;
  order: number;
}

export interface CreateTrackRequest {
  name: string;
  description?: string;
  color?: string;
}

export interface UpdateTrackRequest {
  name: string;
  description?: string;
  color?: string;
}
