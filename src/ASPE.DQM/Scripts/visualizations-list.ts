import Vue from 'vue';
import '@progress/kendo-ui';
import '@progress/kendo-theme-default/dist/all.css';

import { Dialog } from '@progress/kendo-dialog-vue-wrapper';
import { DialogInstaller } from '@progress/kendo-dialog-vue-wrapper';

import { Validator } from '@progress/kendo-validator-vue-wrapper';
import { ValidatorInstaller } from '@progress/kendo-validator-vue-wrapper';
import { debug } from 'util';

import axios from 'axios';
import { join } from 'path';
import { Http2ServerResponse } from 'http2';

Vue.use(DialogInstaller);
Vue.use(ValidatorInstaller);

interface registrationVueDTO {
    id: any,
    requireAuth: boolean,
    linkTitle: string | null,
    description: string | null,
}

new Vue({
    el: '#vue_visualizations',
    data: {
        visualizations: <registrationVueDTO[]>[]
    },
    methods: {
        onRegisterVisualization() {
            window.location.href = '/register-visualization';
            return true;
        },
        onGoToVisualization(data: registrationVueDTO) {
            window.location.href = '/visual/' + data.id;
        }
    },
    beforeMount: function () {
        axios.get('/api/Visualization/List').then((response) => {
            //this.measures = response.data;
            for (let i = 0; i < response.data.length; i++) {
                let dto = response.data[i];
                this.visualizations.push(<registrationVueDTO>{
                    id: dto.id,
                    requireAuth: dto.requireAuth,
                    linkTitle: '<a href="/visual/' + dto.id + '">' + dto.title + (dto.publish ? '' : ' (unpublished)') + '</a>',
                    description:(dto.description != null && dto.description.length > 255) ? dto.description.substring(0,255).trim() + "..." : dto.description
                });

            }
        }).catch((error) => {

        });
    }
});