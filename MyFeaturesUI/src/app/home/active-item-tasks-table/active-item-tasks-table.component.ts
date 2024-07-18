import { CommonModule } from '@angular/common';
import { Component, EventEmitter, inject, Input, Output } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogService } from 'primeng/dynamicdialog';
import { TableModule } from 'primeng/table';
import { filter, take, tap } from 'rxjs';
import { ItemDto, ItemTaskDto } from '../../../infrastructure';
import { DescriptionType } from '../../enum/description-type.enum';
import { ItemExtendedService } from '../../extended-services/item-extended-service';
import { EditItemDialogComponent } from '../edit-item-dialog/edit-item-dialog.component';

@Component({
  selector: 'app-active-item-tasks-table',
  standalone: true,
  imports: [
    CommonModule,
    TableModule,
    ButtonModule,
    TranslateModule
  ],
  templateUrl: './active-item-tasks-table.component.html',
  styleUrl: './active-item-tasks-table.component.scss'
})
export class ActiveItemTasksTableComponent {
  @Input() items!: ItemDto[];
  @Input() columns!: any[];
  @Input() currentDay!: string | null;
  @Input() itemsOrderLocked!: boolean;
  @Input() title!: string;
  @Input() isRecurring!: boolean;
  @Output() currentDayChange = new EventEmitter<any>();

  private confirmationService = inject(ConfirmationService);
  private itemExtendedService = inject(ItemExtendedService);
  private dialogService = inject(DialogService);
  private translate = inject(TranslateService);

  newIndex: number | null = null;
  originalIndex!: number;

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

  assignItemToSelectedWeekday(itemTask: ItemTaskDto) {
    if (this.currentDay) {
      const isLocked$ = itemTask.item?.recurring
        ? this.itemExtendedService.getRecurringItemsOrderLocked$()
        : this.itemExtendedService.getOneTimeItemsOrderLocked$();

      isLocked$.pipe(
        take(1),
        filter(isLocked => isLocked),
        tap(() => this.itemExtendedService.commitItem(itemTask.id!, this.currentDay))
      ).subscribe();
    }
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
      header: this.translate.instant('deleteConfirmation'),
      acceptLabel: this.translate.instant('confirm'),
      rejectLabel: this.translate.instant('cancel'),
      accept: () => {
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

  lockOrUnlockTableOrder(recurring: boolean) {
    const lockState$ = recurring
      ? this.itemExtendedService.getRecurringItemsOrderLocked$()
      : this.itemExtendedService.getOneTimeItemsOrderLocked$();

    lockState$.pipe(
      take(1),
      tap(isLocked => {
        if (recurring) {
          this.itemExtendedService.loadRecurringItems(!isLocked);
        } else {
          this.itemExtendedService.loadOneTimeItems(!isLocked);
        }
      })
    ).subscribe();
  }

  private resetCurrentDay() {
    this.currentDayChange.emit(null);
  }
}
