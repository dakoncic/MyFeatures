import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { EditorModule } from 'primeng/editor';
import { ItemService } from '../../../infrastructure';

@Component({
  selector: 'app-editor',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    EditorModule
  ],
  templateUrl: './editor.component.html',
  styleUrl: './editor.component.scss'
})
export class EditorComponent implements OnInit {
  @Input() editorContent: EditorContent;
  form!: FormGroup;

  private formBuilder = inject(FormBuilder);
  private itemService = inject(ItemService);

  ngOnInit(): void {
    this.form = this.formBuilder.group({
      id: [this.editorContent.id],
      content: [this.editorContent.content]
    });
  }

  saveEditor(): void {
    const updatedEditor: EditorContent = this.form.value;
    if (updatedEditor.id) {
      this.editorService.updateEditor(updatedEditor).subscribe();
    }
  }

  deleteEditor(): void {
    const editorId = this.form.get('id')!.value;
    if (editorId) {
      this.editorService.deleteEditor(editorId).subscribe(() => {
        this.editorDeleted.emit(editorId);
      });
    }
  }
}
