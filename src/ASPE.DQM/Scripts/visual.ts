import axios from 'axios';
import { debug } from 'webpack';

function toggleBookmark(ev: MouseEvent): void {
    let visualID = (<any>ev.currentTarget).attributes["data-visual-id"].nodeValue;
    axios.post("/api/visualization/bookmark/" + visualID).then(function (response: any) {
        let icon = document.getElementById("bookmarkIcon");
        if (icon != null) {
            icon.classList.toggle("fas");
            icon.classList.toggle("far");
        }
    });
};

let btn: HTMLElement = <HTMLElement>document.getElementById("btnFavoriteToggle");
btn.onclick = toggleBookmark;
