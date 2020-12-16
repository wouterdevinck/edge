import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { Device } from './models/device.model';

@Injectable({
  providedIn: 'root'
})
export class ApiService {

  private devicesUrl = 'https://api.wouterdevinck.be/api/v1/devices';  
  private auth = '?subscription-key=xxx';

  constructor(private http: HttpClient) { }

  getDevices(): Observable<Device[]> {
    return this.http.get<Device[]>(`${this.devicesUrl}${this.auth}`);
  }

  sendDeviceCommand(deviceId: string, command: string): void {
    this.http.post(`${this.devicesUrl}/${deviceId}/commands/${command}${this.auth}`, '{}').subscribe()
  }

}
