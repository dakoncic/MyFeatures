import { CommonModule } from '@angular/common';
import { Component, Input, inject } from '@angular/core';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DragDropModule } from 'primeng/dragdrop';
import { DialogService } from 'primeng/dynamicdialog';
import { RippleModule } from 'primeng/ripple';
import { TableModule } from 'primeng/table';
import { Observable } from 'rxjs';
import { ItemDto } from '../../../infrastructure';
import { ItemExtendedService } from '../../extended-services/item-extended-service';
import { EditItemDialogComponent } from '../edit-item-dialog/edit-item-dialog.component';

@Component({
  selector: 'app-todo',
  standalone: true,
  imports: [
    CommonModule,
    TableModule,
    DragDropModule,
    ButtonModule,
    RippleModule
  ],
  templateUrl: './todo.component.html',
  styleUrl: './todo.component.scss'
})
export class TodoComponent {
  private confirmationService = inject(ConfirmationService);
  private itemExtendedService = inject(ItemExtendedService);
  private dialogService = inject(DialogService);

  @Input() items$!: Observable<any[]>;
  @Input() cols!: any[];
  @Input() weekDayDate!: string;

  isDragOver: boolean = false;

  onDragStart(event: DragEvent, rowData: any) {
    // Convert the rowData object to a JSON string
    const data = JSON.stringify(rowData);

    // Use the dataTransfer.setData() method to set the data to be transferred
    // "application/json" is used as a type identifier to signify the type of data being transferred
    event.dataTransfer?.setData('application/json', data);
  }

  dragEnd() {
    console.log('drag end happening');
  }

  onDragOver(event: any) {
    event.preventDefault(); // Necessary to allow the drop
    this.isDragOver = true;
    console.log(this.isDragOver);
  }

  onDrop(event: DragEvent, weekDayDate: string) {
    event.preventDefault();
    event.stopPropagation();

    const data = event.dataTransfer?.getData('application/json');
    const rowData = JSON.parse(data!);

    this.isDragOver = false;
    console.log(weekDayDate);
    console.log(rowData);

  }

  generateCaption(weekDayDate: string): string {
    const dueDate = new Date(weekDayDate);
    dueDate.setHours(0, 0, 0, 0); // Normalize to start of the day, timezone is implicitly UTC

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const tomorrow = new Date();
    tomorrow.setDate(today.getDate() + 1);
    tomorrow.setHours(0, 0, 0, 0);

    if (dueDate.getTime() === today.getTime()) {
      return dueDate.toLocaleDateString('hr-HR', { weekday: 'long' }) + ' (Danas)';
    } else {
      return dueDate.toLocaleDateString('hr-HR', { weekday: 'long' });
    }
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
  }
}
