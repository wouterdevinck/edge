import { Component, OnInit } from '@angular/core';
import { ApiService } from '../api.service';
import { Device } from '../models/device.model';

@Component({
  selector: 'app-devices',
  templateUrl: 'devices.component.html',
  styles: [
  ]
})
export class DevicesComponent implements OnInit {

  devices: Device[] = [];

  constructor(private api: ApiService) { }

  ngOnInit(): void {
    this.getDevices();
  }

  getDevices(): void {
    this.api.getDevices()
      .subscribe(devices => this.devices = devices);
  }

  sendCommand(deviceId: string, command: string): void {
    this.api.sendDeviceCommand(deviceId, command);
    setTimeout(() => {
      this.getDevices(); // TEMP hack to refresh status
    }, 3000)
  }

}
