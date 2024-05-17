export * from './item.service';
import { ItemService } from './item.service';
export * from './notepad.service';
import { NotepadService } from './notepad.service';
export const APIS = [ItemService, NotepadService];
