import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnDestroy, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { EditorModule } from 'primeng/editor';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { Subject, takeUntil } from 'rxjs';
import { NotepadDto } from '../../../infrastructure';
import { NotepadExtendedService } from '../../extended-services/notepad-extended-service';

@Component({
  selector: 'app-notepad',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    EditorModule,
    ButtonModule,
    FormsModule,
    InputNumberModule,
    InputTextModule
  ],
  templateUrl: './notepad.component.html',
  styleUrl: './notepad.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
//za editor moram: npm install quill 1.3.7 (jer se na novom  ne želi bindat vrijednost content-a u p-editor html)
//ovo je bug do njih izgleda
//importat EditorModule
//i dodat "node_modules/quill/dist/quill.snow.css" u angular.json pod "styles"
export class NotepadComponent implements OnInit, OnDestroy {
  @Input() notepad!: NotepadDto;

  private destroy$ = new Subject<void>();

  form!: FormGroup;

  private formBuilder = inject(FormBuilder);
  private notepadExtendedService = inject(NotepadExtendedService);
  private confirmationService = inject(ConfirmationService);
  private cdr = inject(ChangeDetectorRef);

  get content() {
    return this.form.get('content');
  }

  ngOnInit(): void {
    this.form = this.formBuilder.group({
      content: [null],
      rowIndex: [null, Validators.required]
    });

    this.displayNotepad();

    //handling specific bug for "p-editor" where component is not marked
    //as dirty on text change, when using "ChangeDetectionStrategy.OnPush"
    this.form.get('content')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.cdr.detectChanges();
      });
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

    this.notepadExtendedService.updateNotepad(updatedNotepad);
  }

  deleteNotepad() {
    this.confirmationService.confirm({
      header: 'Potvrda brisanja',
      acceptLabel: 'Potvrdi',
      rejectLabel: 'Odustani',
      accept: () => {
        this.notepadExtendedService.deleteNotepad(this.notepad.id!);
      }
    });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
