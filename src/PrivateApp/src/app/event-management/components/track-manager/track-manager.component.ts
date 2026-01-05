import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DragDropModule, CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { TrackService } from '../../services/track.service';
import { Track } from '../../models/track.model';

@Component({
  selector: 'app-track-manager',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DragDropModule],
  templateUrl: './track-manager.component.html',
  styleUrls: ['./track-manager.component.scss']
})
export class TrackManagerComponent implements OnInit {
  @Input() eventId!: string;
  tracks: Track[] = [];
  trackForm: FormGroup;
  editingTrackId: string | null = null;
  isFormVisible = false;

  constructor(
    private trackService: TrackService,
    private fb: FormBuilder
  ) {
    this.trackForm = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      color: ['#000000']
    });
  }

  ngOnInit(): void {
    if (this.eventId) {
      this.loadTracks();
    }
  }

  loadTracks(): void {
    this.trackService.getTracks(this.eventId).subscribe(tracks => {
      this.tracks = tracks;
    });
  }

  showAddForm(): void {
    this.editingTrackId = null;
    this.trackForm.reset({ color: '#000000' });
    this.isFormVisible = true;
  }

  editTrack(track: Track): void {
    this.editingTrackId = track.id;
    this.trackForm.patchValue({
      name: track.name,
      description: track.description,
      color: track.color
    });
    this.isFormVisible = true;
  }

  cancelEdit(): void {
    this.isFormVisible = false;
    this.editingTrackId = null;
  }

  onSubmit(): void {
    if (this.trackForm.valid) {
      const request = this.trackForm.value;
      if (this.editingTrackId) {
        this.trackService.updateTrack(this.eventId, this.editingTrackId, request).subscribe(() => {
          this.loadTracks();
          this.cancelEdit();
        });
      } else {
        this.trackService.createTrack(this.eventId, request).subscribe(() => {
          this.loadTracks();
          this.cancelEdit();
        });
      }
    }
  }

  deleteTrack(trackId: string): void {
    if (confirm('Are you sure you want to delete this track?')) {
      this.trackService.deleteTrack(this.eventId, trackId).subscribe(() => {
        this.loadTracks();
      });
    }
  }

  drop(event: CdkDragDrop<Track[]>): void {
    moveItemInArray(this.tracks, event.previousIndex, event.currentIndex);

    const trackIds = this.tracks.map(t => t.id);
    this.trackService.reorderTracks(this.eventId, trackIds).subscribe();
  }
}
