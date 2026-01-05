import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import { map, mergeMap, catchError } from 'rxjs/operators';
import { SessionService } from '../services/session.service';
import * as ScheduleActions from './schedule.actions';

@Injectable()
export class ScheduleEffects {
  loadSessions$ = createEffect(() => this.actions$.pipe(
    ofType(ScheduleActions.loadSessions),
    mergeMap(action => this.sessionService.getSessions(action.eventId)
      .pipe(
        map(sessions => ScheduleActions.loadSessionsSuccess({ sessions })),
        catchError(error => of(ScheduleActions.loadSessionsFailure({ error })))
      ))
    )
  );

  // FR-307: UI Rollback Logic
  // The updateSession action is handled optimistically in the reducer.
  // If the API call fails, we dispatch updateSessionFailure with the original session
  // so the reducer can roll back the state.
  updateSession$ = createEffect(() => this.actions$.pipe(
    ofType(ScheduleActions.updateSession),
    mergeMap(action => this.sessionService.updateSession(action.session)
      .pipe(
        map(updatedSession => ScheduleActions.updateSessionSuccess({ session: updatedSession })),
        catchError(error => of(ScheduleActions.updateSessionFailure({
          error,
          originalSession: action.originalSession
        })))
      ))
    )
  );

  constructor(
    private actions$: Actions,
    private sessionService: SessionService
  ) {}
}
