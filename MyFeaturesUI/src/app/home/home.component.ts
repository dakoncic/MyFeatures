import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { DividerModule } from 'primeng/divider';
import { DragDropModule } from 'primeng/dragdrop';
import { DialogService } from 'primeng/dynamicdialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectButtonModule } from 'primeng/selectbutton';
import { ToolbarModule } from 'primeng/toolbar';
import { map, of, take } from 'rxjs';
import { ItemExtendedService } from '../extended-services/item-extended-service';
import { NotepadExtendedService } from '../extended-services/notepad-extended-service';
import { ActiveItemTasksTableComponent } from './active-item-tasks-table/active-item-tasks-table.component';
import { EditItemDialogComponent } from './edit-item-dialog/edit-item-dialog.component';
import { NotepadComponent } from './notepad/notepad.component';
import { TodoComponent } from './todo/todo.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    ToolbarModule,
    InputTextModule,
    SelectButtonModule,
    EditItemDialogComponent,
    TodoComponent,
    ActiveItemTasksTableComponent,
    NotepadComponent,
    DividerModule,
    DragDropModule,
    TranslateModule
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
  private itemExtendedService = inject(ItemExtendedService);
  private notepadExtendedService = inject(NotepadExtendedService);
  private dialogService = inject(DialogService);
  private datePipe = inject(DatePipe);
  private translate = inject(TranslateService);

  newIndex: number | null = null;
  originalIndex!: number;

  cols: any[] = [];
  currentDay: string | null = null;

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

    this.cols = [
      { field: 'description' },
      { field: 'dueDate', align: 'right' }
    ];
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

          let dayNameInEnglish = new Intl.DateTimeFormat('en-US', { weekday: 'long' }).format(dateToAdd);

          let dayName = i === 0
            ? this.translate.instant(dayNameInEnglish) + ' (' + this.translate.instant('today') + ')'
            : this.translate.instant(dayNameInEnglish);

          let localDateStr = dateToAdd.toLocaleDateString('en-CA', {
            year: 'numeric', month: '2-digit', day: '2-digit'
          });

          updates.push({
            name: dayName,
            value: localDateStr
          });
        }

        // samo push modificira array ali se ne mijenja referenca što ne okine change detection
        // moram koristit spread syntax da napravi novu referencu
        this.weekdays = [...updates];
      });
  }

  openNew() {
    this.dialogService.open(EditItemDialogComponent, {});
  }

  createNewNotepad() {
    this.notepadExtendedService.createNotepad();
  }
}
