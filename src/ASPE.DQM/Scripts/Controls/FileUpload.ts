import Vue from 'vue';
import '@progress/kendo-ui';
import '@progress/kendo-theme-default/dist/all.css';
import { Component, Prop } from 'vue-property-decorator';
import $ from 'jquery';

import { UploadInstaller, KendoUpload } from '@progress/kendo-upload-vue-wrapper';
import { debug } from 'util';

Vue.use(UploadInstaller)

@Component({
    template: '<kendo-upload ref="upload" ' +
                            'name="files"' +
                            ':localization-select="\'Select File for Upload\'"' +
                            ':show-file-list="false"' +
                            ':multiple="allowMultiple"' +
                            ':async-save-url="saveUrl"' +
                            ':async-chunk-size="25000000"' +
                            ':async-auto-upload="autoUpload"' +
                            ':async-batch="false"' +
                            ':async-concurrent="false"' +
                            ':async-auto-retry-after="300"' +
                            ':async-max-auto-retries="5">' +
                '</kendo-upload>'
})
export default class FileUpload extends Vue {
    @Prop({ default: '/api/Documents/Upload' })
    saveUrl!: string;
    @Prop({ default: false })
    allowMultiple!: boolean;
    @Prop({ default: true })
    autoUpload!: boolean;
    @Prop()
    itemId!: any;
    uploadselectedfiles = "";

    control!: kendo.ui.Widget;

    previouslyHidden: boolean = true;

    onUpload(e) {
        e.data = {
            ItemID: this.itemId
        };
        var element = (<$>this.control.element)[0].parentElement.parentElement.parentElement.children[1];
        if (element.style.display === "none")
            element.style.display = null;
    }

    onSuccess(e) {
        this.$root.$emit('Doc-Uploaded');
        var element = (<$>this.control.element)[0].parentElement.parentElement.parentElement.children[1];
        element.style.display = "none";
    }

    mounted() {
        this.control = (<KendoUpload>this.$refs.upload).kendoWidget();
        this.control.bind("upload", this.onUpload);
        this.control.bind('success', this.onSuccess);
    }
}