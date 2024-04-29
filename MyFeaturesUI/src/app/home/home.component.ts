import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogService } from 'primeng/dynamicdialog';
import { InputTextModule } from 'primeng/inputtext';
import { RippleModule } from 'primeng/ripple';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TableModule } from 'primeng/table';
import { ToolbarModule } from 'primeng/toolbar';
import { map, of, tap } from 'rxjs';
import { ItemTaskDto } from '../../infrastructure';
import { ItemExtendedService } from '../extended-services/item-extended-service';
import { EditItemDialogComponent } from './edit-item-dialog/edit-item-dialog.component';
import { TodoComponent } from './todo/todo.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    TableModule,
    FormsModule,
    ButtonModule,
    //treba ili ne??
    RippleModule,
    ToolbarModule,
    InputTextModule,
    SelectButtonModule,
    EditItemDialogComponent,
    TodoComponent
  ],
  providers: [
    //moram provide-at zbog *null injector error-a*
    DialogService
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  private confirmationService = inject(ConfirmationService);
  private itemExtendedService = inject(ItemExtendedService);
  private dialogService = inject(DialogService);

  editDialogVisible: boolean = false;
  cols: any[] = [];
  currentDay!: string;

  weekdays: any[] = [];

  oneTimeItems$ = this.itemExtendedService.oneTimeItems$;
  recurringItems$ = this.itemExtendedService.recurringItems$;

  weekData$ = this.itemExtendedService.weekData$.pipe(
    tap((data) => console.log(data)),
    map(weekdata => weekdata.map(daydata => ({
      weekDayDate: daydata.weekDayDate!,
      items$: of(daydata.itemTasks!)
    })))
  );

  constructor(
  ) {

  }

  ngOnInit() {
    this.initializeWeekdays();

    this.cols = [
      { field: 'description', header: 'Opis' }
    ];

  }

  initializeWeekdays(): void {
    // Function to add days to a date
    const addDays = (date: Date, days: number): Date => {
      let result = new Date(date);
      result.setDate(result.getDate() + days);
      return result;
    };

    // Generate weekdays starting from today
    for (let i = 0; i < 7; i++) {
      let dateToAdd = addDays(new Date(), i);
      let dayName = i === 0 ? 'Today' : new Intl.DateTimeFormat('hr-HR', { weekday: 'long' }).format(dateToAdd);
      this.weekdays.push({
        name: dayName,
        value: dateToAdd.toISOString() // Full ISO 8601 date and time format
      });
    }
  }


  editItem(itemTask: ItemTaskDto) {
    this.dialogService.open(EditItemDialogComponent, {
      data: {
        itemTask: itemTask
      },
      //header: this.translate.instant('measurement.dialog.manualChannels')
    });
  }

  deleteItem(itemTask: ItemTaskDto) {
    this.confirmationService.confirm({
      header: 'Delete Confirmation',
      message: 'Do you want to delete this record?',
      acceptLabel: 'Potvrdi',
      rejectLabel: 'Odustani',
      accept: () => {
        //obriši i osvježi liste svima
        this.itemExtendedService.deleteItem(itemTask.id!);
      }
    });

    //switchMap će biti unsubscribe-an kada i njegov parent
    //items$ budu unsubscribe-ani, a bit će zbog async pipe-a u html-u
    // ide kroz extended servis, ne lokalno
    // this.items$ = this.itemService.deleteItem(this.itemTask.id!)
    //   .pipe(
    //     switchMap(() => this.itemService.getAllItem())
    //   );
  }

  completeItem(itemTask: ItemTaskDto) {
    this.itemExtendedService.completeItem(itemTask.id!);
  }

  openNew() {
    this.dialogService.open(EditItemDialogComponent, {
      data: {
      },
      //header: this.translate.instant('measurement.dialog.manualChannels')
    });
  }
}
