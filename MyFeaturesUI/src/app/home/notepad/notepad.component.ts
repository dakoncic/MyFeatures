import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { NotepadDto, NotepadService } from '../../../infrastructure';
import { EditorModule } from 'primeng/editor';

@Component({
  selector: 'app-notepad',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    EditorModule
  ],
  templateUrl: './notepad.component.html',
  styleUrl: './notepad.component.scss'
})
export class NotepadComponent implements OnInit {
  @Input() notepad!: NotepadDto;

  form!: FormGroup;

  private formBuilder = inject(FormBuilder);
  private notepadService = inject(NotepadService);

  ngOnInit(): void {
    this.form = this.formBuilder.group({
      content: [null]
    });

    this.displayNotepad();
  }

  displayNotepad(): void {
    this.form.patchValue({
      ...this.notepad
    });
  }

  saveNotepad(): void {
    const updatedNotepad: NotepadDto = {
      ...this.notepad,
      ...this.form.value
    };

    this.notepadService.updateNotepad(updatedNotepad.id!, updatedNotepad).subscribe();
  }

  deleteNotepad(): void {
    this.notepadService.deleteNotepad(this.notepad.id!).subscribe();
  }
}
