import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, switchMap, take } from 'rxjs';
import { ItemDto, ItemService, ItemTaskDto } from '../../infrastructure';

@Injectable({
  providedIn: 'root'
})
export class ItemExtendedService {
  private itemService = inject(ItemService);

  private itemsSourceSubject = new BehaviorSubject<ItemDto[]>([]);
  private weekDaysSourceSubject = new BehaviorSubject<ItemDto[]>([]);

  //ovo se poziva prvi put u trenutku kad se komponenta
  //subscribe-a sa async pipe-om u htmlu
  //ovdje ne može .subscribe() zato što on ne vraća observable
  //a nama je items$ tipa observable gdje se subscribe-amo sa async pipeom
  //behaviorSubject smo stavili zato što će on triggerat get all nakon delete
  //a behavior je zbog tog što želimo da se okine sam prvi put za komponentu
  //zbog default vrijednosti, što će aktivirat switchMap,
  //koji prekida emittanje default i poziva getAllItem.
  //običan Subject neće triggerat na prvi subscribe, nego tek na ".next()"
  items$ = this.itemsSourceSubject
    .pipe(switchMap(() => this.itemService.getAllItem()));

  weekData$ = this.weekDaysSourceSubject
    .pipe(switchMap(() => this.itemService.getItemsForWeekItem()));

  //na create item, za sad osvježavamo sve
  createItem(item: ItemDto) {
    return this.itemService.createItem(item).pipe(
      take(1),
    )
      .subscribe(() => {
        this.itemsSourceSubject.next([]);
      });
  }

  //na update item, za sad osvježavamo sve
  updateItem(item: ItemDto) {
    return this.itemService.updateItem(item.id!, item).pipe(
      take(1),
    )
      .subscribe(() => {
        this.itemsSourceSubject.next([]);
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
        this.itemsSourceSubject.next([]);
      });
  }

  completeItem(itemTask: ItemTaskDto) {
    return this.itemService.commitItemTaskItem(itemTask.id!).pipe(
      take(1),
    )
      .subscribe(() => {
        this.itemsSourceSubject.next([]);
      });
  }
}
