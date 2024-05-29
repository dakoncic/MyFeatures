import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CalendarModule } from 'primeng/calendar';
import { DialogModule } from 'primeng/dialog';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { RadioButtonModule } from 'primeng/radiobutton';
import { RippleModule } from 'primeng/ripple';
import { SelectButtonModule } from 'primeng/selectbutton';
import { Subject, combineLatest, take, takeUntil } from 'rxjs';
import { IntervalType, ItemService, ItemTaskDto } from '../../../infrastructure';
import { DescriptionType } from '../../enum/description-type.enum';
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
    InputTextModule,
    FormsModule,
    RadioButtonModule
  ],
  templateUrl: './edit-item-dialog.component.html',
  styleUrl: './edit-item-dialog.component.scss'
})
export class EditItemDialogComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  itemTask: ItemTaskDto = {}; //trenutno selektiran
  stateOptions: any[] = [{ label: 'Jednokratni', value: false }, { label: 'Ponavljajući', value: true }];

  renewOptions: any[] = [{ label: 'Na krajnji rok', value: true }, { label: 'Na datum izvršenja', value: false }];
  descriptionType!: DescriptionType;
  ingredient!: string;

  form!: FormGroup;
  private formBuilder = inject(FormBuilder);
  private ref = inject(DynamicDialogRef);
  private config = inject(DynamicDialogConfig);
  private itemService = inject(ItemService);
  private itemExtendedService = inject(ItemExtendedService);

  //TO DO: refaktor ovo u generički validator
  get description() {
    return this.form.get('description');
  }

  get intervalValue() {
    return this.form.get('intervalValue');
  }

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
      this.descriptionType = this.config.data.descriptionType;

      this.editItem(this.config.data.itemTask);

      //nema mijenjanja recurringa na edit
      this.form.get('recurring')?.disable();
    }
    else {
      //inače za create disable-a by default
      this.form.get('renewOnDueDate')?.disable();
      this.form.get('intervalType')?.disable();
      this.form.get('intervalValue')?.disable();
    }

    //**update value and validity možda i ne treba jer resetam ako je disabled
    combineLatest([
      this.form.get('recurring')!.valueChanges,
      this.form.get('dueDate')!.valueChanges,
    ])
      .pipe(takeUntil(this.destroy$))
      .subscribe(([recurring, dueDate]) => {
        console.log('changes combine latest');
        if (recurring && dueDate) {
          this.form.get('renewOnDueDate')?.enable();
          //ako je odabrao datum i recurring je, mora odabrat tip sekvence
          this.form.get('renewOnDueDate')?.addValidators(Validators.required);
        } else {
          this.form.get('renewOnDueDate')?.disable();
          this.form.get('renewOnDueDate')?.reset();
        }
      });

    this.form.get('renewOnDueDate')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((renewOnDueDate) => {
        console.log('changes only renewOnDueDate');
        //mora bit eksplicitna provjera null-a zbog boolean true/false
        if (renewOnDueDate !== null) {
          this.form.get('intervalType')?.enable();
          //ako je odabrao datum i recurring je, mora odabrat tip sekvence
          this.form.get('intervalType')?.addValidators(Validators.required);
        } else {
          this.form.get('intervalType')?.disable();
          this.form.get('intervalType')?.reset();
        }
      });

    this.form.get('intervalType')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((intervalType) => {
        console.log('changes only intervalType');
        if (intervalType) {
          this.form.get('intervalValue')?.enable();
          this.form.get('intervalValue')?.addValidators(Validators.required);
        } else {
          //validator automatski maknut
          this.form.get('intervalValue')?.disable();
          this.form.get('intervalValue')?.reset();
        }
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

    const description = this.descriptionType === DescriptionType.OriginalDescription ? itemTask.item!.description : itemTask.description;

    this.form.patchValue({
      description: description,
      recurring: itemTask.item!.recurring,
      renewOnDueDate: itemTask.item!.renewOnDueDate,
      dueDate: itemTask.dueDate,
      intervalValue: itemTask.item!.intervalValue,
      intervalType: itemTask.item!.intervalType
    });
  }

  saveItem() {
    //ako nije dirty onda nemoj zvat backend
    if (this.form.dirty) {

      if (!this.itemTask.id) {
        const itemTask: ItemTaskDto = {

          dueDate: this.form.getRawValue().dueDate,
          description: this.form.getRawValue().description,
          item: {
            description: this.form.getRawValue().description,
            recurring: this.form.getRawValue().recurring, //ovdi je bug na edit jer je disabled pa uzima false, moram get raw value
            renewOnDueDate: this.form.getRawValue().renewOnDueDate,
            intervalValue: this.form.getRawValue().intervalValue,
            intervalType: this.form.getRawValue().intervalType
          }
        };

        this.itemExtendedService.createItem(itemTask)
      } else {

        const itemTask: ItemTaskDto = {
          //prvo stare vrijednosti npr. rowId (concurrency)
          ...this.itemTask,

          dueDate: this.form.getRawValue().dueDate,
          item: {
            ...this.itemTask.item,
            recurring: this.form.getRawValue().recurring,
            renewOnDueDate: this.form.getRawValue().renewOnDueDate,
            intervalValue: this.form.getRawValue().intervalValue,
            intervalType: this.form.getRawValue().intervalType
          }
        };

        //ako je one time item, onda se mora update-at i original i task
        if (itemTask.item!.recurring === false) {
          itemTask.item!.description = this.form.getRawValue().description;
          itemTask.description = this.form.getRawValue().description;
        }
        //ako je update uncommitted item-a (original, ne iz weekdays tablice), onda njega update-at iz forme
        else if (this.descriptionType === DescriptionType.OriginalDescription) {
          itemTask.item!.description = this.form.getRawValue().description;
        } else {
          //inače update-at child item
          itemTask.description = this.form.getRawValue().description;
        }

        this.itemExtendedService.updateItem(itemTask);
      }
    }

    this.itemTask = {}; //resetiraj trenutni edit itemTask
    this.form.reset();

    //provjerit jel potrebno
    this.hideDialog();
  }

  hideDialog(): void {
    this.ref.close();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
