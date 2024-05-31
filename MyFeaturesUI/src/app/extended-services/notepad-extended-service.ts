import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, switchMap, take } from 'rxjs';
import { NotepadDto, NotepadService } from '../../infrastructure';

@Injectable({
  providedIn: 'root'
})
export class NotepadExtendedService {
  private notepadService = inject(NotepadService);

  private notepadsSourceSubject = new BehaviorSubject<NotepadDto[]>([]);

  notepads$ = this.notepadsSourceSubject
    .pipe(switchMap(() => this.notepadService.getAllNotepads()));

  createNotepad() {
    return this.notepadService.createNotepad().pipe(
      take(1),
    )
      .subscribe(() => {
        this.notepadsSourceSubject.next([]);
      });
  }

  updateNotepad(notepad: NotepadDto) {
    return this.notepadService.updateNotepad(notepad.id!, notepad).pipe(
      take(1),
    )
      .subscribe(() => {
        this.notepadsSourceSubject.next([]);
      });
  }

  deleteNotepad(notepadId: number) {
    return this.notepadService.deleteNotepad(notepadId).pipe(
      take(1),
    )
      .subscribe(() => {
        this.notepadsSourceSubject.next([]);
      });
  }
}
