import Vue from 'vue';
import { Component, Prop } from 'vue-property-decorator';
import { KendoTooltip, KendoNotification, KendoPopupsInstaller } from '@progress/kendo-popups-vue-wrapper'
import axios from 'axios';
import moment from 'moment';

interface IDocument {
    removeLink: string;
    id: any;
    name: string;
    size: string;
    mimeType: string;
    createdOn: Date;
    itemID: any
    userName: string;
    firstName: string;
    lastName: string;
}

Vue.use(KendoPopupsInstaller);

@Component({
    template:  '<table class="table table-bordered table-striped">' +
                '<thead>' +
                    '<tr>' +
                        '<td>Title</td>' +
                        '<td class="d-none d-md-table-cell">Size</td>' +
                        '<td class="d-none d-md-table-cell">Created On</td>' +
                        '<td class="d-none d-md-table-cell">Uploaded By</td>' +
                    '</tr>' +
                '</thead>' +
                '<tbody>' +
                    '<tr v-for="doc in Documents">' +
                        '<td>' +
                            '<div class="float-right"><kendo-tooltip :show-on="\'mouseenter\'" v-bind:title="$options.filters.formatfilesize(doc.size) + \'<br/>Created On: \' + $options.filters.formatdate(doc.createdOn) + \'<br />Uploaded By: \' + doc.lastName + \', \' + doc.firstName" :position="\'top\'"> <i class="fas fa-info-circle d-block d-sm-block d-md-none" style="color: #007bff; margin-left: 10px;"></i></kendo-tooltip></div>'+
                            '<i class="fas fa-trash float-right" style="color: #007bff" @click="removeDocument(doc.id)"></i>' +
                            '<span v-html="doc.nameLink"></span>' +
                        '</td>' +
                        '<td class="d-none d-md-table-cell">{{doc.size | formatfilesize}}</td>' +
                        '<td class="d-none d-md-table-cell">{{doc.createdOn | formatdate}}</td>' +
                        '<td class="d-none d-md-table-cell">{{doc.lastName}}, {{doc.firstName}}</td>' +
                    '</tr>' +
                '</tbody>' +
                '</table>'
})
export default class FileList extends Vue {
    @Prop()
    itemId!: any;
    @Prop({ default: '/api/Documents/List' })
    listEndpoint!: string;
    @Prop({ default: false })
    showDelete!: boolean;

    Documents: IDocument[] = [];

    getDocuments() {
        axios.get(this.listEndpoint + "/" + this.itemId).then((response) => {
            this.Documents = [];
            for (var i = 0; i < response.data.length; i++) {
                var dto = response.data[i];
                this.Documents.push(<IDocument>{
                    removeLink: '<button type="button" class="btn btn-danger" @click="removeDocument(doc.id)">Remove</button>',
                    nameLink: '<a v-bind:title="$options.filters.formatfilesize(doc.size) + \' \' + $options.filters.formatdate(doc.createdOn)" href="/api/Documents/Download/' + dto.id + '">' + dto.name + '</a>',
                    id: dto.id,
                    name: dto.name,
                    size: dto.size,
                    mimeType: dto.mimeType,
                    createdOn: dto.createdOn,
                    itemID: dto.itemID,
                    userName: dto.userName,
                    firstName: dto.firstName,
                    lastName: dto.lastName
                });

            }
        }).catch((error) => {

        });
    }

    removeDocument(itemID: any) {
        axios.delete("/api/Documents/Delete/" + itemID).then(() => {
            this.getDocuments();
        });        
    }
    
    mounted() {
        this.$root.$on('Doc-Uploaded', () => {
            this.getDocuments();
        });
    }

    beforeMount() {
        this.getDocuments();
    }
}

Vue.filter("formatfilesize", function (length: number) {
    let kilobyte: number = 1024;
    let megabyte: number = 1024 * 1024;
    let gigabyte: number = 1024 * 1024 * 1024;
    if (length > gigabyte)
        return (length / gigabyte).toFixed(2) + ' Gb';

    if (length > megabyte)
        return (length / megabyte).toFixed(2) + ' Mb';

    if (length > kilobyte)
        return (length / kilobyte).toFixed(2) + ' Kb';

    return length + ' bytes';
});

Vue.filter('formatdate', function (value) {
    if (value) {
        return moment.utc(String(value)).local().format('MM/DD/YYYY hh:mm a')
    }
});