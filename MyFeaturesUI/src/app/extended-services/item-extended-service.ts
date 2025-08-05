import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, switchMap, take } from 'rxjs';
import { CommitItemTaskDto, ItemService, ItemTaskDto, UpdateItemIndexDto, UpdateItemTaskIndexDto } from '../../infrastructure';

@Injectable({
  providedIn: 'root'
})
export class ItemExtendedService {
  private itemService = inject(ItemService);

  private oneTimeItemsSourceSubject = new BehaviorSubject<void>(undefined);
  private recurringItemsSourceSubject = new BehaviorSubject<void>(undefined);
  private weekDaysSourceSubject = new BehaviorSubject<void>(undefined);

  oneTimeItems$ = this.oneTimeItemsSourceSubject.pipe(
    switchMap(() => this.itemService.getOneTimeItemTasks())
  );

  recurringItems$ = this.recurringItemsSourceSubject.pipe(
    switchMap(() => this.itemService.getRecurringItemTasks())
  );

  weekData$ = this.weekDaysSourceSubject.pipe(
    switchMap(() => this.itemService.getCommitedItemsForNextWeek())
  );

  createItem(itemTask: ItemTaskDto) {
    return this.itemService.createItemAndTask(itemTask).pipe(
      take(1),
    )
      .subscribe(() => {
        this.refreshAllItemLists();
      });
  }

  updateItem(itemTask: ItemTaskDto) {
    return this.itemService.updateItemAndTask(itemTask.id!, itemTask).pipe(
      take(1),
    )
      .subscribe(() => {
        this.refreshAllItemLists();
      });
  }

  deleteItem(itemId: number) {
    return this.itemService.deleteItemAndTasks(itemId).pipe(
      take(1),
    )
      .subscribe(() => {
        this.refreshAllItemLists();
      });
  }

  completeItem(itemTaskId: number) {
    return this.itemService.completeItemTask(itemTaskId).pipe(
      take(1),
    )
      .subscribe(() => {
        this.refreshAllItemLists();
      });
  }

  commitItem(itemTaskId: number, commitDay: string | null) {
    const commitItem: CommitItemTaskDto = {
      commitDay: commitDay,
      itemTaskId: itemTaskId,
    };

    return this.itemService.commitItemTask(commitItem).pipe(
      take(1),
    )
      .subscribe(() => {
        this.refreshAllItemLists();
      });
  }

  updateItemIndex(itemId: number, newIndex: number, recurring: boolean) {
    const updatedItemIndex: UpdateItemIndexDto = {
      itemId: itemId,
      newIndex: newIndex,
      recurring: recurring,
    };

    this.itemService.reorderItemInsideGroup(updatedItemIndex).pipe(
      take(1),
    )
      .subscribe(() => {
        if (recurring) {
          this.recurringItemsSourceSubject.next();
        } else {
          this.oneTimeItemsSourceSubject.next();
        }
      });
  }

  updateItemTaskIndex(itemTaskId: number, commitDay: string, newIndex: number) {
    const updatedItemTaskIndex: UpdateItemTaskIndexDto = {
      commitDay: commitDay,
      itemTaskId: itemTaskId,
      newIndex: newIndex
    };

    this.itemService.reorderItemTaskInsideGroup(updatedItemTaskIndex).pipe(
      take(1),
    )
      .subscribe(() => {
        this.weekDaysSourceSubject.next();
      });
  }

  private refreshAllItemLists() {
    this.oneTimeItemsSourceSubject.next();
    this.recurringItemsSourceSubject.next();
    this.weekDaysSourceSubject.next();
  }
}
