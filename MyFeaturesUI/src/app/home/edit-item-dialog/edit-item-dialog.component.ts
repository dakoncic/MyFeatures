import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CalendarModule } from 'primeng/calendar';
import { DialogModule } from 'primeng/dialog';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { RippleModule } from 'primeng/ripple';
import { SelectButtonModule } from 'primeng/selectbutton';
import { Subject, combineLatest, take, takeUntil } from 'rxjs';
import { ItemService, ItemTaskDto } from '../../../infrastructure';
import { ItemExtendedService } from '../../extended-services/item-extended-service';

@Component({
  selector: 'app-edit-item-dialog',
  standalone: true,
  imports: [
    CommonModule,
    DialogModule,
    CalendarModule,
    InputNumberModule,
    ReactiveFormsModule,
    SelectButtonModule,
    RippleModule,
    InputTextModule
  ],
  templateUrl: './edit-item-dialog.component.html',
  styleUrl: './edit-item-dialog.component.scss'
})
export class EditItemDialogComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  itemTask: ItemTaskDto = {}; //trenutno selektiran
  stateOptions: any[] = [{ label: 'One time task', value: true }, { label: 'Repeating', value: false }];

  form!: FormGroup;
  private formBuilder = inject(FormBuilder);
  private ref = inject(DynamicDialogRef);
  private config = inject(DynamicDialogConfig);
  private itemService = inject(ItemService)
  private itemExtendedService = inject(ItemExtendedService);

  //TO DO: refaktor ovo u generički validator
  get description() {
    return this.form.get('description');
  }

  get daysBetweenRepeat() {
    return this.form.get('daysBetweenRepeat');
  }

  ngOnInit() {
    this.form = this.formBuilder.group({
      description: ['', Validators.required],
      oneTimeTask: [true],
      dueDate: [null],
      daysBetweenRepeat: [null, Validators.required],
    });

    //ako je edit, povuci s backenda i prikaži na formi
    if (this.config.data?.itemTask) {
      this.editItem(this.config.data.itemTask);
    }
    else {
      //inače za create disable-a by default
      this.form.get('daysBetweenRepeat')?.disable();
    }

    combineLatest([
      this.form.get('oneTimeTask')!.valueChanges,
      this.form.get('dueDate')!.valueChanges,
    ])
      .pipe(takeUntil(this.destroy$))
      .subscribe(([oneTime, dueDate]) => {
        if (!oneTime && dueDate) {
          this.form.get('daysBetweenRepeat')?.enable();
        } else {
          this.form.get('daysBetweenRepeat')?.disable();
        }

        this.form.get('daysBetweenRepeat')?.updateValueAndValidity();
      });
  }

  completeItem(itemTask: ItemTaskDto) {
    this.itemExtendedService.completeItem(itemTask.id!);
  }

  //povlači itemTask za edit s backenda
  editItem(itemTask: ItemTaskDto) {
    this.itemService.getItemTaskItem(itemTask.id!)
      .pipe(take(1))
      .subscribe((itemTask) => {
        this.displayItem(itemTask);
      });
  }

  //popunjava se forma za edit
  displayItem(itemTask: ItemTaskDto): void {
    this.form.reset();

    this.itemTask = itemTask;
    this.form.patchValue({
      ...itemTask
    });
  }

  saveItem() {
    //ako nije dirty onda nemoj zvat backend
    if (this.form.dirty) {

      const itemTask: ItemTaskDto = {
        //prvo stare vrijednosti npr. rowId (concurrency)
        ...this.itemTask,
        //onda vrijednosti forme
        ...this.form.value
      };

      if (!this.itemTask.id) {
        this.itemExtendedService.createItem(itemTask)
      } else {
        this.itemExtendedService.updateItem(itemTask);
      }
    }

    this.itemTask = {}; //resetiraj trenutni edit itemTask
    this.form.reset();

    //provjerit jel potrebno
    this.hideDialog();
  }

  hideDialog(): void {
    //zašto Cancel button reload-a stranicu
    this.ref.close();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
