import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, switchMap, take } from 'rxjs';
import { CommitItemTaskDto, ItemService, ItemTaskDto, UpdateItemTaskIndexDto, WeekDayDto } from '../../infrastructure';

@Injectable({
  providedIn: 'root'
})
export class ItemExtendedService {
  private itemService = inject(ItemService);

  private oneTimeItemsSourceSubject = new BehaviorSubject<ItemTaskDto[]>([]);
  private recurringItemsSourceSubject = new BehaviorSubject<ItemTaskDto[]>([]);
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
    .pipe(switchMap(() => this.itemService.getOneTimeItemTasksItem()));

  recurringItems$ = this.recurringItemsSourceSubject
    .pipe(switchMap(() => this.itemService.getRecurringItemTasksItem()));

  weekData$ = this.weekDaysSourceSubject
    .pipe(switchMap(() => this.itemService.getCommitedItemsForNextWeekItem()));

  //na create item, za sad osvježavamo sve
  createItem(itemTask: ItemTaskDto) {
    return this.itemService.createItem(itemTask).pipe(
      take(1),
    )
      .subscribe(() => {
        //ovdje ćemo za prvu sve liste osvježit
        console.log('refetch');
        this.oneTimeItemsSourceSubject.next([]);
        this.recurringItemsSourceSubject.next([]);
        this.weekDaysSourceSubject.next([]);
      });
  }

  //na update item, za sad osvježavamo sve
  updateItem(itemTask: ItemTaskDto) {
    return this.itemService.updateItem(itemTask.id!, itemTask).pipe(
      take(1),
    )
      .subscribe(() => {
        //ovdje ćemo za prvu sve liste osvježit

        this.oneTimeItemsSourceSubject.next([]);
        this.recurringItemsSourceSubject.next([]);
        this.weekDaysSourceSubject.next([]);
      });
  }

  //delete item mora osvježavati weekDays i items liste,
  //kasnije možda optimizirat ovisno odakle je pozvana metoda
  //za sad samo jedna postoji koja sve osvježava
  deleteItem(itemId: number) {
    return this.itemService.deleteItem(itemId).pipe(
      take(1),
    )
      .subscribe(() => {
        //ovdje ćemo za prvu sve liste osvježit

        this.oneTimeItemsSourceSubject.next([]);
        this.recurringItemsSourceSubject.next([]);
        this.weekDaysSourceSubject.next([]);
      });
  }

  completeItem(itemTaskId: number) {
    return this.itemService.completeItemTaskItem(itemTaskId).pipe(
      take(1),
    )
      .subscribe(() => {
        //ovdje ćemo za prvu sve liste osvježit

        this.oneTimeItemsSourceSubject.next([]);
        this.recurringItemsSourceSubject.next([]);
        this.weekDaysSourceSubject.next([]);
      });
  }

  commitItem(itemTaskId: number, commitDay: string) {
    const commitItem: CommitItemTaskDto = {
      commitDay: commitDay,
      itemTaskId: itemTaskId,
    };

    return this.itemService.commitItemTaskItem(commitItem).pipe(
      take(1),
    )
      .subscribe(() => {
        //ovdje ćemo za prvu sve liste osvježit

        this.oneTimeItemsSourceSubject.next([]);
        this.recurringItemsSourceSubject.next([]);
        this.weekDaysSourceSubject.next([]);
      });
  }

  updateItemTaskIndex(itemTaskId: number, commitDay: string, newIndex: number) {
    const updatedItemTaskIndex: UpdateItemTaskIndexDto = {
      commitDay: commitDay,
      itemTaskId: itemTaskId,
      newIndex: newIndex
    };

    return this.itemService.updateItemTaskIndexItem(updatedItemTaskIndex).pipe(
      take(1),
    )
      .subscribe(() => {
        this.weekDaysSourceSubject.next([]);
      });
  }
}
