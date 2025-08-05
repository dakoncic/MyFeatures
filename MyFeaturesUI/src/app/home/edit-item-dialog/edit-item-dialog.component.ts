import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { CalendarModule } from 'primeng/calendar';
import { DialogModule } from 'primeng/dialog';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { RadioButtonModule } from 'primeng/radiobutton';
import { SelectButtonModule } from 'primeng/selectbutton';
import { Subject, combineLatest, take, takeUntil } from 'rxjs';
import { IntervalType, ItemService, ItemTaskDto } from '../../../infrastructure';
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
    InputTextModule,
    FormsModule,
    RadioButtonModule,
    TranslateModule
  ],
  templateUrl: './edit-item-dialog.component.html',
  styleUrl: './edit-item-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EditItemDialogComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  form!: FormGroup;
  private formBuilder = inject(FormBuilder);
  private ref = inject(DynamicDialogRef);
  private config = inject(DynamicDialogConfig);
  private itemService = inject(ItemService);
  private itemExtendedService = inject(ItemExtendedService);
  private translate = inject(TranslateService);

  itemTask: ItemTaskDto = {}; //trenutno selektiran
  stateOptions: any[] = [
    { label: this.translate.instant('editItem.oneTime'), value: false },
    { label: this.translate.instant('editItem.recurring'), value: true }
  ];

  renewOptions: any[] = [
    { label: this.translate.instant('editItem.onDueDate'), value: true },
    { label: this.translate.instant('editItem.onCompletionDate'), value: false }
  ];
  ingredient!: string;

  intervalType = IntervalType;

  ngOnInit() {
    this.form = this.formBuilder.group({
      description: ['', Validators.required],
      recurring: [false, Validators.required],
      dueDate: [null],
      renewOnDueDate: [null],
      intervalType: [null],
      intervalValue: [null]
    });

    //ako je edit, povuci s backenda i prikaži na formi
    if (this.config.data?.itemTask) {
      this.editItem(this.config.data.itemTask);

      //nema mijenjanja recurringa na edit
      this.form.get('recurring')?.disable();
    }
    else {
      //inače za create disable-a by default
      this.disableNonRecurringFields();
    }

    this.setupValueChangeHandlers();
  }

  private disableNonRecurringFields() {
    this.form.get('renewOnDueDate')?.disable();
    this.form.get('intervalType')?.disable();
    this.form.get('intervalValue')?.disable();
  }

  private setupValueChangeHandlers() {
    combineLatest([
      this.form.get('recurring')!.valueChanges,
      this.form.get('dueDate')!.valueChanges,
    ])
      .pipe(takeUntil(this.destroy$))
      .subscribe(([recurring, dueDate]) => {
        if (recurring && dueDate) {
          this.form.get('renewOnDueDate')?.enable();
        } else {
          this.form.get('renewOnDueDate')?.disable();
          this.form.get('renewOnDueDate')?.reset();
        }
      });

    this.form.get('renewOnDueDate')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((renewOnDueDate) => {
        if (renewOnDueDate !== null) {
          this.form.get('intervalType')?.enable();
          //ako je odabrao datum i recurring je, mora odabrat tip sekvence
          this.form.get('intervalType')?.addValidators(Validators.required);
        } else {
          this.form.get('intervalType')?.disable();
          this.form.get('intervalType')?.reset();
        }

        this.form.get('intervalType')?.updateValueAndValidity();
      });

    this.form.get('intervalType')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((intervalType) => {
        if (intervalType) {
          this.form.get('intervalValue')?.enable();
          this.form.get('intervalValue')?.addValidators(Validators.required);
        } else {
          //validator automatski maknut
          this.form.get('intervalValue')?.disable();
          this.form.get('intervalValue')?.reset();
        }

        this.form.get('intervalValue')?.updateValueAndValidity();
      });
  }

  completeItem(itemTask: ItemTaskDto) {
    this.itemExtendedService.completeItem(itemTask.id!);
  }

  editItem(itemTask: ItemTaskDto) {
    this.itemService.getItemTaskById(itemTask.id!)
      .pipe(take(1))
      .subscribe((itemTask) => {
        this.displayItem(itemTask);
      });
  }

  displayItem(itemTask: ItemTaskDto): void {
    this.itemTask = itemTask;

    const description = itemTask.committedDate ? itemTask.description : itemTask.item!.description;

    this.form.patchValue({
      description: description,
      recurring: itemTask.item!.recurring,
      renewOnDueDate: itemTask.item!.renewOnDueDate,
      dueDate: itemTask.dueDate ? new Date(itemTask.dueDate) : null,
      intervalValue: itemTask.item!.intervalValue,
      intervalType: itemTask.item!.intervalType
    });
  }

  saveItem() {
    if (this.form.dirty) {
      let itemTask: ItemTaskDto;

      if (!this.itemTask.id) {
        itemTask = {
          description: this.form.getRawValue().description,
          //spremam samo datum bez vremenske komponente (gledam datum iz kalendara, vremenska zona nije važna) ".toLocale(en-CA)"
          dueDate: this.form.getRawValue().dueDate ? this.form.getRawValue().dueDate.toLocaleDateString('en-CA') : null,
          item: {
            ...this.form.getRawValue()
          }
        };

        this.itemExtendedService.createItem(itemTask)
      } else {

        itemTask = {
          ...this.itemTask,
          dueDate: this.form.getRawValue().dueDate ? this.form.getRawValue().dueDate.toLocaleDateString('en-CA') : null,
          item: {
            ...this.itemTask.item,
            renewOnDueDate: this.form.getRawValue().renewOnDueDate,
            intervalType: this.form.getRawValue().intervalType,
            intervalValue: this.form.getRawValue().intervalValue
          }
        };

        this.updateDescriptions(itemTask);

        this.itemExtendedService.updateItem(itemTask);
      }
    }

    this.hideDialog();
  }

  private updateDescriptions(itemTask: ItemTaskDto) {
    const description = this.form.getRawValue().description;

    //za one time, update se radi na oba descriptiona
    if (!itemTask.item!.recurring) {
      itemTask.item!.description = description;
      itemTask.description = description;
    }
    //ako je update original item-a (ne iz weekdays tablice), onda samo njega update-at iz forme
    else if (itemTask.committedDate) {
      itemTask.description = description;
    } else {
      //inače update-at child item iz weekdays
      itemTask.item!.description = description;
    }
  }

  hideDialog(): void {
    this.ref.close();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
