import Vue from 'vue';
import '@progress/kendo-ui';
import '@progress/kendo-theme-default/dist/all.css';
import { Component, Prop } from 'vue-property-decorator';
import { $ } from 'jQuery';

import { Upload, UploadInstaller } from '@progress/kendo-upload-vue-wrapper';
import { KendoDialog, DialogInstaller } from '@progress/kendo-dialog-vue-wrapper';
import { debug, error } from 'util';
import axios from 'axios';
import { ApiResult } from './data-common';

Vue.use(UploadInstaller);
Vue.use(DialogInstaller);

export class UploadResponseState {

    constructor(readonly errors: string[] | null, readonly fileUid: any | null, readonly metricName: string | null, readonly metricID: any | null) {
    }

    get success(): boolean {
        return error.length == 0;
    }
}

interface kendoUploadResponse {
    operation: string;
    response: UploadResult;
}

class UploadResult extends ApiResult<any> {
    readonly fileUid: string | null | undefined;
    readonly uploaded: boolean | undefined;
    readonly metricID: any | null | undefined;
    readonly metricName: string | null | undefined;
}

let initialUploadResponse = function () { return new UploadResponseState([], null, "", null); };

new Vue({
    el: '#vue_submit_measures',
    data: function () {
        return {
            kendoOptions: {
                uploadLocalization: {
                    select: "Select File..."
                },
                allowedExtensions: [".xlsx", ".json"]
            },
            uploadResponse: initialUploadResponse(),
            lastUploadedMetric: ""
        };
    },
    mounted: function () {
        //@ts-ignore;
        this.uploadWidget = this.$refs.upload.kendoWidget();
        //@ts-ignore;
        this.confirmationDialogWidget = this.$refs.confirmationDialog.kendoWidget();
    },
    methods: {
        onUploadSuccess: function (e: kendoUploadResponse) {
            if (e.operation != "upload") {
                return;
            }

            this.lastUploadedMetric = "";
            this.uploadResponse = new UploadResponseState(e.response.errors||null, e.response.fileUid, e.response.metricName||null, e.response.metricID);

            //@ts-ignore
            this.uploadWidget.clearAllFiles();            

            if (e.response.errors != null && e.response.errors.length > 0) {
                let message = <string[]>[];
                message.push("<div>There were validation errors with the uploaded document:<ul>");
                e.response.errors.forEach((err) => {
                    message.push("<li>" + err + "</li>");
                });
                message.push("</ul></div>");

                kendo.alert(message.join(''));
            } else {
                //@ts-ignore
                this.confirmationDialogWidget.open();
            }
        },
        onUploadError: function (e) {
            if (e.operation != "upload")
                return;

            this.lastUploadedMetric = "";
            this.uploadResponse = initialUploadResponse();

            let rsp = JSON.parse(e.XMLHttpRequest.response);

            if (rsp.errors != null && rsp.errors.length > 0) {
                let message = <string[]>[];
                message.push("<div>There were validation errors with the uploaded document:<ul>");
                rsp.errors.forEach((err) => {
                    message.push("<li>" + err + "</li>");
                });
                message.push("</ul></div>");

                kendo.alert(message.join(''));
            }
        },
        onContinueUpload: function (e) {
            let self = this;
            axios.patch('/api/measures/accept/' + this.uploadResponse.fileUid).then((response) => {
                self.lastUploadedMetric = self.uploadResponse.metricName || "";
                self.uploadResponse = initialUploadResponse();
                return true;
            }).catch((reason) => {
                debugger;
                });
        },
        onCancelUpload: function (e) {
            let self = this;
            axios.delete('/api/measures/reject/' + this.uploadResponse.fileUid)
                .then((response) => {
                    self.lastUploadedMetric = "";
                    self.uploadResponse = initialUploadResponse();
                    return true;
                }).catch((reason) => { debugger; });
        }
    }
});

