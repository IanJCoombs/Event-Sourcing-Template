import { Component, OnInit, Inject, Input } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormControl } from '@angular/forms';

type NodeState = {
    id: string;
    name: string;
}

@Component({
    selector: 'app-node',
    styleUrls: ['./node.component.css'],
    templateUrl: './node.component.html'
})


export class NodeComponent implements OnInit {
    @Input() id: string;
    http: HttpClient;
    baseUrl: string;
    name = new FormControl();
    change = false;
    currentState: NodeState;
    errors : string[] = []

    constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
        this.http = http;
        this.baseUrl = baseUrl;
    }

    ngOnInit(): void {
        if (this.id != null && this.id != "") {
            this.http.post(this.baseUrl + 'Nodes/GetNode/', { Id: this.id }).subscribe((result: NodeState) => {
                this.currentState = {
                    id: this.id,
                    name: result.name
                }
                
                this.name.setValue(this.currentState.name);

            }, error => console.error(error));
        }
    }

    validName() {
        return this.name.value != null && this.name.value != "" && this.name.value != this.currentState.name;
    }

    onAnyChange(event: Event) {
        this.change = true;
        this.errors = [];
        
        if (!this.validName()) {
            this.errors.push("Name cannot be empty");
        }
    }

    onSaveClick(event: Event) {
        let actions : (() => void)[] = [];

        if (this.validName()) {
            actions.push(() => {
                this.http.post(this.baseUrl + 'Nodes/SetNodeName/', { Id: this.id, Name: this.name.value }).subscribe(result => {
                }, error => console.error(error));
            })
        }
        
        actions.forEach(action => action());
        this.change = false;
    }
}
