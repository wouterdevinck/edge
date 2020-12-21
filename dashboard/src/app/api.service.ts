import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Device } from './models/device.model';
import * as signalR from '@microsoft/signalr';

@Injectable({
  providedIn: 'root'
})
export class ApiService {

  private baseUrl = 'https://api.wouterdevinck.be/api/v1/';
  private auth = '?subscription-key=';

  constructor(private http: HttpClient) { }

  getDevices(): Observable<Device[]> {
    return this.http.get<Device[]>(`${this.baseUrl}devices${this.auth}`);
  }

  sendDeviceCommand(deviceId: string, command: string): void {
    this.http.post(`${this.baseUrl}devices/${deviceId}/commands/${command}${this.auth}`, '{}').subscribe()
  }

  subscribeTwinUpdates(): Observable<Device> {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.baseUrl}${this.auth}`)
      .configureLogging(signalR.LogLevel.Information)
      .build();
    async function start() {
      try {
        await connection.start();
        console.log("Connected");
      } catch (err) {
        console.log(err);
        setTimeout(start, 5000);
      }
    };
    return new Observable((observer) => {
      connection.onclose(start);
      connection.on("twinupdates", (device) => {
        console.log(`Device ${device.deviceId} twin update recieved`);
        observer.next(device);
      });
      start();
    });
  }

}
