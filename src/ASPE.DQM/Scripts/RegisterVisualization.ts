import Vue from 'vue';
import '@progress/kendo-ui';
import '@progress/kendo-theme-default/dist/all.css';
import $ from 'jquery';

import { Dialog, DialogInstaller } from '@progress/kendo-dialog-vue-wrapper';
import { Validator, ValidatorInstaller } from '@progress/kendo-validator-vue-wrapper';
import { debug } from 'util';

import axios from 'axios';
import { join } from 'path';
import { Http2ServerResponse } from 'http2';

Vue.use(DialogInstaller);
Vue.use(ValidatorInstaller);

interface registrationVueDTO {
    id: string | null,
    title: string | null,
    appID: string | null,
    sheetID: string | null,
    description: string | null,
    requireAuth: boolean,
    publish: boolean
}

new Vue({
    el: '#vue_registration',
    data: <registrationVueDTO>{
        id: null,
        title: null,
        appID: null,
        sheetID: null,
        description: null,
        requireAuth: false,
        publish: false
    },
    methods: {
        onRegister(e) {
            let validator = (<any>this).kendoValidator;
            if (validator.validate()) {
                let dto = {
                    title: this.title,
                    appID: this.appID,
                    sheetID: this.sheetID,
                    description: this.description,
                    requireAuth: this.requireAuth,
                    publish: this.publish
                };

                //set empty objects to null to satisfy PMN validation
                for (let prp in Object.keys(dto)) {
                    let val = dto[prp];
                    if (val != null && val.trim().length == 0) {
                        dto[prp] = null;
                    }
                }

                axios.post('/api/visualization/register', dto).then((response) => {
                    window.location.href = '/visualizations';
                    return true;
                }).catch((error) => {
                    
                });
            }

            return false;
        },
        onEdit(e) {
            let validator = (<any>this).kendoValidator;
            if (validator.validate()) {
                let dto = {
                    id: this.id,
                    title: this.title,
                    appID: this.appID,
                    sheetID: this.sheetID,
                    description: this.description,
                    requireAuth: this.requireAuth,
                    publish: this.publish
                };

                //set empty objects to null to satisfy PMN validation
                for (let prp in Object.keys(dto)) {
                    let val = dto[prp];
                    if (val != null && val.trim().length == 0) {
                        dto[prp] = null;
                    }
                }

                axios.post('/api/visualization/edit', dto).then((response) => {
                    window.location.href = '/visualizations';
                    return true;

                }).catch((error) => {

                });
            }

            return false;
        },
        onDelete(e) {
            let visualID = this.id;
            //@ts-ignore;
            kendo.confirm("Are you sure that you wish to delete this Visualization?").then(function () {
                axios.delete('/api/visualization/delete/' + visualID)
                    .then((response) => {
                        window.location.href = '/visualizations';
                        return true;
                    });
            },
                function () {
                    return false;
                });
        }
    },
    beforeMount: function () {
        //@ts-ignore;
        let id = document.getElementById("vue_registration").getAttribute("data-visual-id");
        if (id) {
            axios.get('/api/visualization/' + id).then((response) => {
                let returnDTO = response.data;
                this.id = returnDTO.id;
                this.title = returnDTO.title;
                this.appID = returnDTO.appID;
                this.sheetID = returnDTO.sheetID;
                this.description = returnDTO.description;
                this.publish = returnDTO.publish;
                this.requireAuth = returnDTO.requireAuth;
            });
        }
    }
});