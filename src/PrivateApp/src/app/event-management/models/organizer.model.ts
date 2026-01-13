export enum OrganizerRole {
  Owner = 0,
  Admin = 1,
  Collaborator = 2,
}

export interface EventOrganizer {
  personId: string;
  firstName: string;
  lastName: string;
  email: string;
  role: OrganizerRole;
  photoUrl?: string;
}

export interface AddOrganizerRequest {
  email: string;
  firstName: string;
  lastName: string;
  role: OrganizerRole;
}
