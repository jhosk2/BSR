import { Component, Inject } from '@angular/core';
import { Http } from '@angular/http';

@Component({
    selector: 'script-detail',
    templateUrl: './script.component.html'
})
export class ScriptComponent {

    private script: string;


    
    constructor(http: Http, @Inject('BASE_URL') baseUrl: string) {
        http.get(baseUrl + `api/ScriptManager/${this.script}`).subscribe(result => {
            
        }, error => console.error(error));
    }
}
