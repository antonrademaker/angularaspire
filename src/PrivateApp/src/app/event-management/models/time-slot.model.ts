export enum TimeSlotType {
  Session = 'Session',
  Break = 'Break',
  Lunch = 'Lunch',
  Keynote = 'Keynote',
  Networking = 'Networking',
  Workshop = 'Workshop'
}

export interface TimeSlot {
  id: string;
  eventId: string;
  name: string;
  startTime: string;
  endTime: string;
  type: TimeSlotType;
  isEventLevel: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateTimeSlotRequest {
  name: string;
  startTime: string;
  endTime: string;
  type: TimeSlotType;
  isEventLevel: boolean;
}

export interface UpdateTimeSlotRequest {
  name: string;
  startTime: string;
  endTime: string;
  type: TimeSlotType;
  isEventLevel: boolean;
}
