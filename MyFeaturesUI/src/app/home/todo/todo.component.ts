import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DragDropModule } from 'primeng/dragdrop';
import { DialogService } from 'primeng/dynamicdialog';
import { TableModule } from 'primeng/table';
import { Observable } from 'rxjs';
import { ItemTaskDto } from '../../../infrastructure';
import { ItemExtendedService } from '../../extended-services/item-extended-service';
import { APPLICATION_JSON } from '../../shared/constants';
import { EditItemDialogComponent } from '../edit-item-dialog/edit-item-dialog.component';

@Component({
  selector: 'app-todo',
  standalone: true,
  imports: [
    CommonModule,
    TableModule,
    DragDropModule,
    ButtonModule
  ],
  providers: [DatePipe],
  templateUrl: './todo.component.html',
  styleUrl: './todo.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TodoComponent {
  private confirmationService = inject(ConfirmationService);
  private itemExtendedService = inject(ItemExtendedService);
  private dialogService = inject(DialogService);
  private datePipe = inject(DatePipe);
  private translate = inject(TranslateService);

  @Input() items$!: Observable<any[]>;
  @Input() weekDayDate!: string;
  @Input() cols!: any[];

  isDragOver = false;
  newIndex: number | null = null;
  originalIndex!: number;

  onDragStart(event: DragEvent, rowData: any, index: number) {
    // Convert the rowData object to a JSON string
    const data = JSON.stringify(rowData);

    // Use the dataTransfer.setData() method to set the data to be transferred
    // "application/json" is used as a type identifier to signify the type of data being transferred
    event.dataTransfer?.setData(APPLICATION_JSON, data);

    this.originalIndex = index;
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

    const data = event.dataTransfer?.getData(APPLICATION_JSON);
    const rowData = JSON.parse(data!);

    //null je kad dropam item al nije promjenio poziciju ili ga pomičem na drugi dan#
    if (this.newIndex !== null && this.newIndex !== this.originalIndex) {
      const formattedDate = this.formatDate(rowData.committedDate);
      this.itemExtendedService.updateItemTaskIndex(rowData.id, formattedDate, this.newIndex);
    }

    //logika za commitanje itema na neki drugi dan#
    const committedDate = this.formatDate(rowData.committedDate);
    const weekDayDateFormatted = this.formatDate(this.weekDayDate);

    //sad provjera ako item želim prebacit na neki drugi dan, onda zovi backend#
    if (committedDate !== weekDayDateFormatted) {
      this.itemExtendedService.commitItem(rowData.id, this.weekDayDate);
    }

    this.newIndex = null;
  }

  onRowReorder(event: any) {
    this.newIndex = event.dropIndex;
  }

  formatDate(dateString: string): string {
    return this.datePipe.transform(dateString, 'yyyy-MM-dd')!;
  }

  generateCaption(weekDayDate: string): string {
    const dueDate = new Date(weekDayDate);
    dueDate.setHours(0, 0, 0, 0); // Set to the start of the day, timezone is UTC

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const optionsWeekday: Intl.DateTimeFormatOptions = { weekday: 'long' };
    const formattedWeekday = this.translate.instant(dueDate.toLocaleDateString('en-US', optionsWeekday));

    // Manually format the date to avoid extra spaces and exclude the year
    const day = ('0' + dueDate.getDate()).slice(-2);
    const month = ('0' + (dueDate.getMonth() + 1)).slice(-2);
    const formattedDate = `${day}.${month}.`;

    if (dueDate.getTime() === today.getTime()) {
      return `${formattedWeekday}, ${formattedDate} (${this.translate.instant('today')})`;
    } else {
      return `${formattedWeekday}, ${formattedDate}`;
    }
  }

  completeItem(itemTask: ItemTaskDto) {
    this.itemExtendedService.completeItem(itemTask.id!);
  }

  editItem(itemTask: ItemTaskDto) {
    this.dialogService.open(EditItemDialogComponent, {
      data: {
        itemTask: itemTask
      }
    });
  }

  returnItemTaskToGroup(itemTask: ItemTaskDto) {
    this.itemExtendedService.commitItem(itemTask.id!, null);
  }

  deleteItem(itemTask: ItemTaskDto) {
    this.confirmationService.confirm({
      header: this.translate.instant('deleteConfirmation'),
      acceptLabel: this.translate.instant('confirm'),
      rejectLabel: this.translate.instant('cancel'),
      accept: () => {
        this.itemExtendedService.deleteItem(itemTask.item!.id!);
      }
    });
  }
}
