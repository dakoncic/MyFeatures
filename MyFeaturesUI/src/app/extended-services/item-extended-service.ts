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

  //ovo se poziva prvi put u trenutku kad se komponenta
  //subscribe-a sa async pipe-om u htmlu
  //ovdje ne može .subscribe() zato što on ne vraća observable
  //a nama je items$ tipa observable gdje se subscribe-amo sa async pipeom
  //behaviorSubject smo stavili zato što će on triggerat get all nakon delete
  //a behavior je zbog tog što želimo da se okine sam prvi put za komponentu
  //zbog default vrijednosti, što će aktivirat switchMap,
  //koji prekida emittanje default i poziva getAllItem.
  //običan Subject neće triggerat na prvi subscribe, nego tek na ".next()"
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

  //na create item, za sad osvježavamo sve
  createItem(itemTask: ItemTaskDto) {
    return this.itemService.createItemTask(itemTask).pipe(
      take(1),
    )
      .subscribe(() => {
        //ovdje ćemo za prvu sve liste osvježit
        this.oneTimeItemsSourceSubject.next(true);
        this.recurringItemsSourceSubject.next(true);
        this.weekDaysSourceSubject.next([]);
      });
  }

  //na update item, za sad osvježavamo sve
  updateItem(itemTask: ItemTaskDto) {
    return this.itemService.updateItemTask(itemTask.id!, itemTask).pipe(
      take(1),
    )
      .subscribe(() => {
        //ovdje ćemo za prvu sve liste osvježit

        this.oneTimeItemsSourceSubject.next(true);
        this.recurringItemsSourceSubject.next(true);
        this.weekDaysSourceSubject.next([]);
      });
  }

  //delete item mora osvježavati weekDays i items liste,
  //kasnije možda optimizirat ovisno odakle je pozvana metoda
  //za sad samo jedna postoji koja sve osvježava
  deleteItem(itemId: number) {
    return this.itemService.deleteItemTask(itemId).pipe(
      take(1),
    )
      .subscribe(() => {
        //ovdje ćemo za prvu sve liste osvježit
        this.oneTimeItemsSourceSubject.next(true);
        this.recurringItemsSourceSubject.next(true);
        this.weekDaysSourceSubject.next([]);
      });
  }

  completeItem(itemTaskId: number) {
    return this.itemService.completeItemTask(itemTaskId).pipe(
      take(1),
    )
      .subscribe(() => {
        //ovdje ćemo za prvu sve liste osvježit

        this.oneTimeItemsSourceSubject.next(true);
        this.recurringItemsSourceSubject.next(true);
        this.weekDaysSourceSubject.next([]);
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
        //ovdje ćemo za prvu sve liste osvježit

        this.oneTimeItemsSourceSubject.next(true);
        this.recurringItemsSourceSubject.next(true);
        this.weekDaysSourceSubject.next([]);
      });
  }

  updateItemIndex(itemId: number, newIndex: number, recurring: boolean) {
    const updatedItemIndex: UpdateItemIndexDto = {
      itemId: itemId,
      newIndex: newIndex,
      recurring: recurring,
    };

    return this.itemService.updateItemIndex(updatedItemIndex).pipe(
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

    return this.itemService.updateItemTaskIndex(updatedItemTaskIndex).pipe(
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
}
