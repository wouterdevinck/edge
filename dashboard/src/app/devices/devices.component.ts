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
    this.api.subscribeTwinUpdates().subscribe(device => {
      this.devices.filter(x => x.deviceId == device.deviceId)[0].twin = device.twin;
    });
  }

  getDevices(): void {
    this.api.getDevices().subscribe(devices => this.devices = devices);
  }

  sendCommand(deviceId: string, command: string): void {
    this.api.sendDeviceCommand(deviceId, command);
  }

}
