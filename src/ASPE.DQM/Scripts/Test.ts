import Vue from 'vue';

import FileUpload from './Controls/FileUpload';
import FileList from './Controls/FileList'

Vue.component("file-upload", FileUpload);
Vue.component("file-list", FileList);

new Vue({
    el: '#testVue'
});