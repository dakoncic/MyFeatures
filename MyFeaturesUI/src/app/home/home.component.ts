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
import { ItemDto } from '../../infrastructure';
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
  currentDay!: number;

  weekdays: any[] = [
    { name: 'Today', value: 1 },
    { name: 'Tuesday', value: 2 },
    { name: 'Wendesday', value: 3 },
    { name: 'Thursday', value: 4 },
    { name: 'Friday', value: 5 },
    { name: 'Saturday', value: 6 },
    { name: 'Sunday', value: 7 }

  ];

  items$ = this.itemExtendedService.items$;

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
    this.cols = [
      { field: 'description', header: 'Opis' }
    ];

  }

  editItem(item: ItemDto) {
    this.dialogService.open(EditItemDialogComponent, {
      data: {
        item: item
      },
      //header: this.translate.instant('measurement.dialog.manualChannels')
    });
  }

  deleteItem(item: ItemDto) {
    this.confirmationService.confirm({
      header: 'Delete Confirmation',
      message: 'Do you want to delete this record?',
      acceptLabel: 'Potvrdi',
      rejectLabel: 'Odustani',
      accept: () => {
        //obriši i osvježi liste svima
        this.itemExtendedService.deleteItem(item.id!);
      }
    });

    //switchMap će biti unsubscribe-an kada i njegov parent
    //items$ budu unsubscribe-ani, a bit će zbog async pipe-a u html-u
    // ide kroz extended servis, ne lokalno
    // this.items$ = this.itemService.deleteItem(this.item.id!)
    //   .pipe(
    //     switchMap(() => this.itemService.getAllItem())
    //   );
  }

  completeItem(item: ItemDto) {
    this.itemExtendedService.completeItem(item);
  }

  openNew() {
    this.dialogService.open(EditItemDialogComponent, {
      data: {
      },
      //header: this.translate.instant('measurement.dialog.manualChannels')
    });
  }
}
