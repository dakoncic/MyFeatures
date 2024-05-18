import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { EditorModule } from 'primeng/editor';
import { NotepadDto, NotepadService } from '../../../infrastructure';

@Component({
  selector: 'app-notepad',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    EditorModule,
    ButtonModule,
    FormsModule
  ],
  templateUrl: './notepad.component.html',
  styleUrl: './notepad.component.scss'
})
//za editor moram: npm install quill 1.3.7 (jer se na novom  ne želi bindat vrijednost content-a u p-editor html)
//ovo je bug do njih izgleda
//importat EditorModule
//i dodat "node_modules/quill/dist/quill.snow.css" u angular.json pod "styles"
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
    console.log(this.notepad);
    this.form.patchValue({
      content: this.notepad.content
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
