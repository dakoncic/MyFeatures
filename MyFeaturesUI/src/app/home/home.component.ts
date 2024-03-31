import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { CalendarModule } from 'primeng/calendar';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { RippleModule } from 'primeng/ripple';
import { SelectButtonModule } from 'primeng/selectbutton';
import { TableModule } from 'primeng/table';
import { ToolbarModule } from 'primeng/toolbar';
import { combineLatest, switchMap, take } from 'rxjs';
import { ItemDTO, ItemService } from '../../infrastructure';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    TableModule,
    ReactiveFormsModule,
    ButtonModule,
    RippleModule,
    ToolbarModule,
    InputTextModule,
    DialogModule,
    SelectButtonModule,
    CalendarModule,
    InputNumberModule
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
  providers: [
    MessageService
  ]
})
export class HomeComponent implements OnInit {
  editDialogVisible: boolean = false;
  deleteDialogVisible: boolean = false;
  item: ItemDTO = {}; //trenutno selektiran
  selectedItems: ItemDTO[] = [];
  submitted: boolean = false;
  cols: any[] = [];
  stateOptions: any[] = [{ label: 'One time task', value: true }, { label: 'Repeating', value: false }];

  form!: FormGroup;
  private formBuilder = inject(FormBuilder);
  private itemService = inject(ItemService);

  items$ = this.itemService.getAllItem();

  constructor(
  ) {

  }


  //TO DO: refaktor ovo u generički validator
  get description() {
    return this.form.get('description');
  }

  get daysBetweenRepeat() {
    return this.form.get('daysBetweenRepeat');
  }

  ngOnInit() {
    this.form = this.formBuilder.group({
      description: ['', Validators.required],
      oneTimeTask: [true],
      dueDate: [null],
      daysBetweenRepeat: [null, Validators.required],
    });

    this.cols = [
      { field: 'description', header: 'Description' }
    ];

    this.form.get('daysBetweenRepeat')?.disable();

    //unsubscribe-at se moram, memory leak
    combineLatest([
      this.form.get('oneTimeTask')!.valueChanges,
      this.form.get('dueDate')!.valueChanges,
    ]).subscribe(([oneTime, dueDate]) => {
      if (!oneTime && dueDate) {
        this.form.get('daysBetweenRepeat')?.enable();
      } else {
        this.form.get('daysBetweenRepeat')?.disable();
      }

      this.form.get('daysBetweenRepeat')?.updateValueAndValidity();
    });

  }

  openNew() {
    this.editDialogVisible = true;
  }

  completeItem(item: ItemDTO) {
    this.itemService.completeItem(item.id!, item)
      .pipe(
        switchMap(() => this.itemService.getAllItem())
      );
  }

  editItem(item: ItemDTO) {
    this.itemService.getItem(item.id!)
      .pipe(take(1))
      .subscribe((daq) => {
        this.displayItem(daq);
      });
  }

  displayItem(item: ItemDTO): void {
    this.form.reset();

    this.item = item;
    this.form.patchValue({
      ...item
    });

    this.editDialogVisible = true;
  }

  deleteItem(itemId: number) {
    this.deleteDialogVisible = true;
  }

  confirmDelete() {
    console.log('deleted');

    this.items$ = this.itemService.deleteItem(this.item.id!)
      .pipe(
        switchMap(() => this.itemService.getAllItem())
      );

    this.deleteDialogVisible = false;
  }

  hideDialog() {
    console.log('hiding');
    this.editDialogVisible = false;
  }

  saveItem() {
    //ako nije dirty onda nemoj zvat backend
    if (this.form.dirty) {

      const item: ItemDTO = {
        //prvo stare vrijednosti npr. rowId (concurrency)
        ...this.item,
        //onda vrijednosti forme
        ...this.form.value
      };

      if (!this.item.id) {
        this.items$ = this.itemService.createItem(item)
          .pipe(
            //switchMap će biti unsubscribe-an kada i njegov parent
            //items$ budu unsubscribe-ani, a bit će zbog async pipe-a u html-u
            switchMap(() => this.itemService.getAllItem())
          );
      } else {
        this.items$ = this.itemService.updateItem(this.item.id!, item)
          .pipe(
            switchMap(() => this.itemService.getAllItem())
          );
      }
    }

    this.item = {}; //resetiraj trenutni edit item
    this.form.reset();
    this.editDialogVisible = false;
  }

}
