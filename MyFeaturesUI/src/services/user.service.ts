import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { User } from '../models/user';

//ovaj servis je samo primjer kako se koristi ako ne koristim codegen za open api
//model folder isto samo primjer
@Injectable({ providedIn: 'root' })
export class UserService {
  constructor(private http: HttpClient) { }
  //naznačit u blog postu da se obavezno mora importat environment.ts a ne devleopment!
  //jer se radi file replacements, bez toga se neće moć napraviti
  getAll(): Observable<User[]> {
    return this.http.get<User[]>(`${environment.apiUrl}/api/user/GetAll`);
  }

  createPost(user: User): Observable<User> {
    return this.http.post<User>(`${environment.apiUrl}/api/user/CreatePost`, user);
  }
}
