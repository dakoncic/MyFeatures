import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DividerModule } from 'primeng/divider';
import { DragDropModule } from 'primeng/dragdrop';
import { DialogService } from 'primeng/dynamicdialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TableModule } from 'primeng/table';
import { ToolbarModule } from 'primeng/toolbar';
import { filter, map, of, take, tap } from 'rxjs';
import { ItemTaskDto } from '../../infrastructure';
import { DescriptionType } from '../enum/description-type.enum';
import { ItemTaskType } from '../enum/item-task-type.enum';
import { ItemExtendedService } from '../extended-services/item-extended-service';
import { NotepadExtendedService } from '../extended-services/notepad-extended-service';
import { EditItemDialogComponent } from './edit-item-dialog/edit-item-dialog.component';
import { NotepadComponent } from './notepad/notepad.component';
import { TodoComponent } from './todo/todo.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    TableModule,
    FormsModule,
    ButtonModule,
    ToolbarModule,
    InputTextModule,
    SelectButtonModule,
    EditItemDialogComponent,
    TodoComponent,
    NotepadComponent,
    DividerModule,
    DragDropModule
  ],
  providers: [
    //moram provide-at zbog *null injector error-a*
    DialogService,
    DatePipe
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomeComponent implements OnInit {
  private confirmationService = inject(ConfirmationService);
  private itemExtendedService = inject(ItemExtendedService);
  private notepadExtendedService = inject(NotepadExtendedService);
  private dialogService = inject(DialogService);
  private datePipe = inject(DatePipe);

  newIndex: number | null = null;
  originalIndex!: number;

  cols: any[] = [];
  currentDay: string | null = null;
  itemTaskType = ItemTaskType;

  isOneTimeItemsLocked!: boolean;

  weekdays: any[] = [];

  oneTimeItems$ = this.itemExtendedService.oneTimeItems$.pipe(
    map(oneTimeTaskItems => oneTimeTaskItems.map(taskItem => ({
      ...taskItem,
      description: taskItem.item?.description, //original od Itema ide ovdje
      dueDate: taskItem.dueDate ? this.datePipe.transform(taskItem.dueDate, 'dd.MM.yy') : null
    })))
  );

  recurringItems$ = this.itemExtendedService.recurringItems$.pipe(
    map(recurringTaskItems => recurringTaskItems.map(taskItem => ({
      ...taskItem,
      description: taskItem.item?.description, //original od Itema ide ovdje
      dueDate: taskItem.dueDate ? this.datePipe.transform(taskItem.dueDate, 'dd.MM.yy') : null
    })))
  );

  oneTimeItemsOrderLocked$ = this.itemExtendedService.getOneTimeItemsOrderLocked$();
  recurringItemsOrderLocked$ = this.itemExtendedService.getRecurringItemsOrderLocked$();

  weekData$ = this.itemExtendedService.weekData$.pipe(
    map(weekdata => weekdata.map(daydata => ({
      weekDayDate: daydata.weekDayDate!,
      items$: of(daydata.itemTasks!).pipe(
        map(itemTasks => itemTasks.map(itemTask => ({
          ...itemTask,
          dueDate: itemTask.dueDate ? this.datePipe.transform(itemTask.dueDate, 'dd.MM.yy') : null
        })))
      )
    })))
  );

  notepads$ = this.notepadExtendedService.notepads$;

  ngOnInit() {
    this.initializeWeekdays();

    this.oneTimeItemsOrderLocked$.pipe(tap(isLocked => {
      this.isOneTimeItemsLocked = isLocked;
    }));

    this.cols = [
      { field: 'description' },
      { field: 'dueDate', align: 'right' }
    ];
  }

  onDayChange(event: any) {
    //console.log(event.value);
  }

  assignItemToSelectedWeekday(itemTask: ItemTaskDto, commitDay: string) {
    if (this.currentDay) {
      const isLocked$ = itemTask.item?.recurring
        ? this.itemExtendedService.getRecurringItemsOrderLocked$()
        : this.itemExtendedService.getOneTimeItemsOrderLocked$();

      isLocked$.pipe(
        take(1),
        //zove tap samo ako je isLocked "true"
        filter(isLocked => isLocked),
        tap(() => this.itemExtendedService.commitItem(itemTask.id!, commitDay))
      ).subscribe();
    }
  }

  initializeWeekdays(): void {
    const addDays = (date: Date, days: number): Date => {
      let result = new Date(date);
      result.setDate(result.getDate() + days);
      return result;
    };

    this.weekData$
      .pipe(take(1))
      .subscribe(weekData => {
        const updates = [];
        for (let i = 0; i < weekData.length; i++) {
          let dateToAdd = addDays(new Date(), i);
          let dayName = i === 0
            ? new Intl.DateTimeFormat('hr-HR', { weekday: 'long' }).format(dateToAdd) + ' (danas)'
            : new Intl.DateTimeFormat('hr-HR', { weekday: 'long' }).format(dateToAdd);

          let localDateStr = dateToAdd.toLocaleDateString('en-CA', {
            year: 'numeric', month: '2-digit', day: '2-digit'
          });

          updates.push({
            name: dayName,
            value: localDateStr
          });
        }

        //samo push modificira array ali se ne mijenja referenca što ne okine change detection
        //moram koristit spread syntax da napravi novu referencu#
        this.weekdays = [...updates];
      });
  }

  editItem(itemTask: ItemTaskDto) {
    this.resetCurrentDay();

    this.dialogService.open(EditItemDialogComponent, {
      data: {
        descriptionType: DescriptionType.OriginalDescription,
        itemTask: itemTask
      }
    });
  }

  deleteItem(itemTask: ItemTaskDto) {
    this.resetCurrentDay();

    this.confirmationService.confirm({
      header: 'Potvrda brisanja',
      acceptLabel: 'Potvrdi',
      rejectLabel: 'Odustani',
      accept: () => {
        //obriši i osvježi liste svima
        this.itemExtendedService.deleteItem(itemTask.item!.id!);
      }
    });

    //ovo je primjer ako svu logiku radim kroz samo ovu komponentu za delete i get all
    //switchMap će biti unsubscribe-an kada i njegov parent
    //items$ budu unsubscribe-ani, a bit će zbog async pipe-a u html-u
    // ide kroz extended servis, ne lokalno
    // this.items$ = this.itemService.deleteItem(this.itemTask.id!)
    //   .pipe(
    //     switchMap(() => this.itemService.getAllItem())
    //   );
  }

  completeItem(itemTask: ItemTaskDto) {
    this.resetCurrentDay();

    this.itemExtendedService.completeItem(itemTask.id!);
  }

  lockOrUnlockTableOrder(itemTask: ItemTaskType) {
    const lockState$ = itemTask === ItemTaskType.OneTimeItemTask
      ? this.itemExtendedService.getOneTimeItemsOrderLocked$()
      : this.itemExtendedService.getRecurringItemsOrderLocked$();

    lockState$.pipe(
      take(1),
      //side logika za "obične" void metode
      tap(isLocked => {
        if (itemTask === ItemTaskType.OneTimeItemTask) {
          this.itemExtendedService.loadOneTimeItems(!isLocked);
        } else {
          this.itemExtendedService.loadRecurringItems(!isLocked);
        }
      })
    ).subscribe();
  }

  onRowReorder(event: any) {
    this.newIndex = event.dropIndex;
  }

  onDragStart(event: DragEvent, rowData: any, index: number) {
    // Convert the rowData object to a JSON string
    const data = JSON.stringify(rowData);

    // Use the dataTransfer.setData() method to set the data to be transferred
    // "application/json" is used as a type identifier to signify the type of data being transferred
    event.dataTransfer?.setData('application/json', data);

    this.originalIndex = index;
  }

  onDrop(event: DragEvent, recurring: boolean) {
    event.preventDefault(); //just in case ako neki browser ne dopušta

    const data = event.dataTransfer?.getData('application/json');
    const rowData = JSON.parse(data!);

    //null je kad dropam item al nije promjenio poziciju ili ga pomičem na drugi dan#
    if (this.newIndex !== null && this.newIndex !== this.originalIndex) {
      this.itemExtendedService.updateItemIndex(rowData.item.id, this.newIndex, recurring);
    }

    this.newIndex = null;
  }

  openNew() {
    this.dialogService.open(EditItemDialogComponent, {
      data: {
      },
      //header: this.translate.instant('measurement.dialog.manualChannels')
    });
  }

  createNewNotepad() {
    this.notepadExtendedService.createNotepad();
  }

  private resetCurrentDay() {
    this.currentDay = null;
  }
}
