import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, switchMap, take } from 'rxjs';
import { CommitItemTaskDto, ItemService, ItemTaskDto, UpdateItemIndexDto, UpdateItemTaskIndexDto, WeekDayDto } from '../../infrastructure';

@Injectable({
  providedIn: 'root'
})
export class ItemExtendedService {
  private itemService = inject(ItemService);

  private oneTimeItemsSourceSubject = new BehaviorSubject<boolean>(true);
  private recurringItemsSourceSubject = new BehaviorSubject<boolean>(true);

  //refaktor i weekdays da je boolean sa nekim default npr. null?
  private weekDaysSourceSubject = new BehaviorSubject<WeekDayDto[]>([]);

  oneTimeItems$ = this.oneTimeItemsSourceSubject
    .pipe(
      switchMap((isLocked: boolean) =>
        isLocked
          ? this.itemService.getOneTimeItemTasks()
          : this.itemService.getOneTimeItemTasksWithWeekdays()
      )
    );

  recurringItems$ = this.recurringItemsSourceSubject
    .pipe(
      switchMap((isLocked: boolean) =>
        isLocked
          ? this.itemService.getRecurringItemTasks()
          : this.itemService.getRecurringItemTasksWithWeekdays()
      )
    );

  weekData$ = this.weekDaysSourceSubject
    .pipe(switchMap(() => this.itemService.getCommitedItemsForNextWeek()));

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
          this.recurringItemsSourceSubject.next(false);
        } else {
          this.oneTimeItemsSourceSubject.next(false);
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
        this.weekDaysSourceSubject.next([]);
      });
  }

  loadOneTimeItems(isLocked: boolean) {
    this.oneTimeItemsSourceSubject.next(isLocked);
  }

  loadRecurringItems(isLocked: boolean) {
    this.recurringItemsSourceSubject.next(isLocked);
  }

  getOneTimeItemsOrderLocked$(): Observable<boolean> {
    return this.oneTimeItemsSourceSubject.asObservable();
  }

  getRecurringItemsOrderLocked$(): Observable<boolean> {
    return this.recurringItemsSourceSubject.asObservable();
  }

  private refreshAllItemLists() {
    this.oneTimeItemsSourceSubject.next(true);
    this.recurringItemsSourceSubject.next(true);
    this.weekDaysSourceSubject.next([]);
  }
}
