import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { OrganizerService } from '../../services/organizer.service';
import { EventOrganizer, OrganizerRole } from '../../models/organizer.model';

@Component({
  selector: 'app-organizer-list',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule
  ],
  templateUrl: './organizer-list.component.html',
  styleUrls: ['./organizer-list.component.scss']
})
export class OrganizerListComponent implements OnInit {
  @Input() eventId: string | null = null;
  organizers: EventOrganizer[] = [];
  displayedColumns: string[] = ['name', 'email', 'role', 'actions'];
  addForm: FormGroup;
  isAdding = false;
  roles = Object.values(OrganizerRole).filter(value => typeof value === 'number') as number[];

  constructor(
    private organizerService: OrganizerService,
    private fb: FormBuilder,
    private snackBar: MatSnackBar
  ) {
    this.addForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      role: [OrganizerRole.Collaborator, Validators.required]
    });
  }

  ngOnInit(): void {
    if (this.eventId) {
      this.loadOrganizers();
    }
  }

  loadOrganizers(): void {
    if (!this.eventId) return;
    this.organizerService.getOrganizers(this.eventId).subscribe({
      next: (data) => this.organizers = data,
      error: (err) => console.error('Failed to load organizers', err)
    });
  }

  toggleAdd(): void {
    this.isAdding = !this.isAdding;
    if (!this.isAdding) {
      this.addForm.reset({ role: OrganizerRole.Collaborator });
    }
  }

  addOrganizer(): void {
    if (this.addForm.invalid || !this.eventId) return;

    this.organizerService.addOrganizer(this.eventId, this.addForm.value).subscribe({
      next: () => {
        this.snackBar.open('Organizer added successfully', 'Close', { duration: 3000 });
        this.loadOrganizers();
        this.toggleAdd();
      },
      error: (err) => {
        console.error('Failed to add organizer', err);
        this.snackBar.open('Failed to add organizer', 'Close', { duration: 3000 });
      }
    });
  }

  removeOrganizer(personId: string): void {
    if (!this.eventId || !confirm('Are you sure you want to remove this organizer?')) return;

    this.organizerService.removeOrganizer(this.eventId, personId).subscribe({
      next: () => {
        this.snackBar.open('Organizer removed successfully', 'Close', { duration: 3000 });
        this.loadOrganizers();
      },
      error: (err) => {
        console.error('Failed to remove organizer', err);
        this.snackBar.open('Failed to remove organizer', 'Close', { duration: 3000 });
      }
    });
  }

  getRoleName(role: number): string {
    return OrganizerRole[role];
  }
}
