import { Component, Inject, Input, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormControl } from '@angular/forms';

@Component({
    selector: 'app-nodes',
    styleUrls: ['./nodes.component.css'],
    templateUrl: './nodes.component.html'
})

export class NodesComponent {
    http: HttpClient;
    baseUrl: string;
    nodes : Map<string, any> = new Map();

    constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
        this.http = http;
        this.baseUrl = baseUrl
    }

    ngOnInit(): void {
        this.http.post(this.baseUrl + 'Nodes/GetActiveNodeIds/', {}).subscribe((result: string[]) => {
            result.forEach(id => this.nodes.set(id,{}));
        }, error => console.error(error));
    }

    onCreateClick(event: Event) {
        this.http.post(this.baseUrl + 'Nodes/Create/', {}).subscribe((result : string) => {
            this.nodes.set(result, {});
        }, error => console.error(error));
    }

    getNodeIds() {
        let keys = Array.from(this.nodes.keys());
        return keys.sort((a, b) => a.localeCompare(b));
    }
}
