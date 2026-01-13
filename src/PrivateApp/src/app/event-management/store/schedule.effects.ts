import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import { map, mergeMap, catchError } from 'rxjs/operators';
import { SessionService } from '../services/session.service';
import { UpdateSessionRequest } from '../models/session.model';
import * as ScheduleActions from './schedule.actions';

@Injectable()
export class ScheduleEffects {
  private actions$ = inject(Actions);
  private sessionService = inject(SessionService);

  loadSessions$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ScheduleActions.loadSessions),
      mergeMap((action) =>
        this.sessionService.getSessions(action.eventId).pipe(
          map((sessions) => ScheduleActions.loadSessionsSuccess({ sessions })),
          catchError((error) => of(ScheduleActions.loadSessionsFailure({ error }))),
        ),
      ),
    ),
  );

  // FR-307: UI Rollback Logic
  // The updateSession action is handled optimistically in the reducer.
  // If the API call fails, we dispatch updateSessionFailure with the original session
  // so the reducer can roll back the state.
  updateSession$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ScheduleActions.updateSession),
      mergeMap((action) => {
        const request: UpdateSessionRequest = {
          title: action.session.title,
          shortCode: action.session.shortCode,
          abstract: action.session.abstract,
          duration: action.session.duration,
          level: action.session.level,
          language: action.session.language,
          capacity: action.session.capacity,
        };
        return this.sessionService
          .updateSession(action.session.eventId, action.session.id, request)
          .pipe(
            map(() => ScheduleActions.updateSessionSuccess({ session: action.session })),
            catchError((error) =>
              of(
                ScheduleActions.updateSessionFailure({
                  error,
                  originalSession: action.originalSession,
                }),
              ),
            ),
          );
      }),
    ),
  );
}
