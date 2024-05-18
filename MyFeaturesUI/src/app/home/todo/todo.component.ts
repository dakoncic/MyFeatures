import { CommonModule, DatePipe } from '@angular/common';
import { Component, ElementRef, Input, ViewChild, inject } from '@angular/core';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DragDropModule } from 'primeng/dragdrop';
import { DialogService } from 'primeng/dynamicdialog';
import { RippleModule } from 'primeng/ripple';
import { TableModule } from 'primeng/table';
import { Observable } from 'rxjs';
import { ItemTaskDto } from '../../../infrastructure';
import { DescriptionType } from '../../enum/description-type.enum';
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
  providers: [DatePipe],
  templateUrl: './todo.component.html',
  styleUrl: './todo.component.scss'
})
export class TodoComponent {
  @ViewChild('dropZoneRef') dropZoneRef!: ElementRef;
  private confirmationService = inject(ConfirmationService);
  private itemExtendedService = inject(ItemExtendedService);
  private dialogService = inject(DialogService);
  private datePipe = inject(DatePipe);

  @Input() items$!: Observable<any[]>;
  @Input() cols!: any[];
  @Input() weekDayDate!: string;

  isDragOver = false;
  newIndex: number | null = null;

  onDragStart(event: DragEvent, rowData: any) {
    // Convert the rowData object to a JSON string
    const data = JSON.stringify(rowData);

    // Use the dataTransfer.setData() method to set the data to be transferred
    // "application/json" is used as a type identifier to signify the type of data being transferred
    event.dataTransfer?.setData('application/json', data);
  }

  //ovo mi ne treba
  dragEnd() {
    //console.log('drag end happening');
  }

  onDragOver(event: DragEvent) {
    event.preventDefault(); //just in case ako neki browser ne dopušta

    //isDragOver je inicijalno false da tablica nema obrub
    //kada počnem radit drag, poprimi true, ali kad kursor izađe iz droppable zone
    //makne se 'p-draggable-enter' klasa sama zbog PrimeNG-a.
    //ali property ostane true. (neće se odma syncat isDragOver i [ngClass])
    //na onDrop, postavi se na false, što onda makne klasu.
    //moram ovdje postavit true, jer ako je inicijalno false
    //a onDrop postavi na false, change detection neće registrirat
    //promjenu
    //onDragLeave radi konflike sa onDragOver ako je drag zone i drop zone isti
    //kao u mom slučaju

    this.isDragOver = true;
  }

  onDrop(event: DragEvent) {
    event.preventDefault(); //just in case ako neki browser ne dopušta
    //moramo ovdje stavit false inače bi ostao border
    this.isDragOver = false;

    const data = event.dataTransfer?.getData('application/json');
    const rowData = JSON.parse(data!);

    //potencijalno napravit if else, a ne 2 if, testirat prvo
    if (this.newIndex !== null) {
      const formattedDate = this.formatDate(rowData.committedDate);
      this.itemExtendedService.updateItemTaskIndex(rowData.id, formattedDate, this.newIndex);
      this.newIndex = null;
    }

    //logika za commitanje itema na neki drugi dan#
    const committedDate = this.formatDate(rowData.committedDate);
    const weekDayDateFormatted = this.formatDate(this.weekDayDate);

    //sad provjera ako item želim prebacit na neki drugi dan, onda zovi backend#
    if (committedDate !== weekDayDateFormatted) {
      this.itemExtendedService.commitItem(rowData.id, this.weekDayDate);
    }
  }

  onRowReorder(event: any) {
    this.newIndex = event.dropIndex;
  }

  formatDate(dateString: string): string {
    return this.datePipe.transform(dateString, 'yyyy-MM-dd')!;
  }

  generateCaption(weekDayDate: string): string {
    const dueDate = new Date(weekDayDate);
    dueDate.setHours(0, 0, 0, 0); // postavi na početak dana, timezone je UTC

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const tomorrow = new Date();
    tomorrow.setDate(today.getDate() + 1);
    tomorrow.setHours(0, 0, 0, 0);

    if (dueDate.getTime() === today.getTime()) {
      return dueDate.toLocaleDateString('hr-HR', { weekday: 'long' }) + ' (danas)';
    } else {
      return dueDate.toLocaleDateString('hr-HR', { weekday: 'long' });
    }
  }

  completeItem(itemTask: ItemTaskDto) {
    this.itemExtendedService.completeItem(itemTask.id!);
  }

  editItem(itemTask: ItemTaskDto) {
    this.dialogService.open(EditItemDialogComponent, {
      data: {
        descriptionType: DescriptionType.TaskItemDescription,
        itemTask: itemTask
      },
      //header: this.translate.instant('measurement.dialog.manualChannels')
    });
  }

  returnItemTaskToGroup(itemTask: ItemTaskDto) {
    this.itemExtendedService.commitItem(itemTask.id!, null);
  }

  deleteItem(itemTask: ItemTaskDto) {
    this.confirmationService.confirm({
      header: 'Delete Confirmation',
      message: 'Do you want to delete this record?',
      acceptLabel: 'Potvrdi',
      rejectLabel: 'Odustani',
      accept: () => {
        //obriši i osvježi liste svima
        this.itemExtendedService.deleteItem(itemTask.item!.id!);
      }
    });
  }
}
