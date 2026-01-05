import { createAction, props } from '@ngrx/store';
import { Session } from '../models/session.model';

export const loadSessions = createAction(
  '[Schedule] Load Sessions',
  props<{ eventId: string }>()
);

export const loadSessionsSuccess = createAction(
  '[Schedule] Load Sessions Success',
  props<{ sessions: Session[] }>()
);

export const loadSessionsFailure = createAction(
  '[Schedule] Load Sessions Failure',
  props<{ error: any }>()
);

// Optimistic Update Actions
export const updateSession = createAction(
  '[Schedule] Update Session',
  props<{ session: Session, originalSession: Session }>()
);

export const updateSessionSuccess = createAction(
  '[Schedule] Update Session Success',
  props<{ session: Session }>()
);

export const updateSessionFailure = createAction(
  '[Schedule] Update Session Failure',
  props<{ error: any, originalSession: Session }>()
);
