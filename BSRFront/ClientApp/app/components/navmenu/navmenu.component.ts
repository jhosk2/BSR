import { Component, Inject } from '@angular/core';
import { Http } from '@angular/http';

@Component({
    selector: 'nav-menu',
    templateUrl: './navmenu.component.html',
    styleUrls: ['./navmenu.component.css']
})
export class NavMenuComponent {

    scripts: string[];

    constructor(http: Http, @Inject('BASE_URL') baseUrl: string) {
        http.get(baseUrl + 'api/ScriptManager/ScriptList').subscribe(result => {
            this.scripts = result.json() as string[];
        }, error => console.error(error));
    }
}
